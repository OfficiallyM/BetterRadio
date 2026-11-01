using NVorbis;
using NVorbis.Contracts;
using System;

namespace BetterRadio.Audio.Ogg
{
	internal class OggVorbisCodec : IOggCodec
	{
		private const int ChunkFrames = 1024;

		private readonly StreamDecoder _decoder;
		private readonly EmitHandler _emit;
		private readonly float[] _buffer;

		internal OggVorbisCodec(Func<byte[]> nextPacket, int serial, EmitHandler emit)
		{
			_emit = emit;

			// NVorbis's own Ogg reader cannot follow a live stream across chain boundaries, so only its decoder is used
			// and packets come from the shared demuxer instead.
			_decoder = new StreamDecoder(new PacketProvider(nextPacket, serial));
			_buffer = new float[ChunkFrames * _decoder.Channels];
		}

		public bool DecodeNext()
		{
			int read = _decoder.Read(_buffer, 0, _buffer.Length);
			if (read <= 0) return false;

			int channels = _decoder.Channels;
			_emit(_buffer, 0, read / channels, _decoder.SampleRate, channels, 0, OggChannelOrder.RightIndex(channels));
			return true;
		}

		public void Dispose()
		{
			_decoder.Dispose();
		}

		private class PacketProvider : NVorbis.Contracts.IPacketProvider
		{
			private readonly Func<byte[]> _nextPacket;
			private Packet _peeked;

			internal PacketProvider(Func<byte[]> nextPacket, int serial)
			{
				_nextPacket = nextPacket;
				StreamSerial = serial;
			}

			public bool CanSeek => false;
			public int StreamSerial { get; }

			public IPacket GetNextPacket()
			{
				IPacket packet = PeekNextPacket();
				_peeked = null;
				return packet;
			}

			public IPacket PeekNextPacket()
			{
				if (_peeked == null)
				{
					byte[] data = _nextPacket();
					if (data != null)
						_peeked = new Packet(data);
				}

				return _peeked;
			}

			public long SeekTo(long granulePos, int preRoll, GetPacketGranuleCount getPacketGranuleCount) => throw new NotSupportedException();
			public long GetGranuleCount() => throw new NotSupportedException();
		}

		private class Packet : DataPacket
		{
			private readonly byte[] _data;
			private int _position;

			internal Packet(byte[] data)
			{
				_data = data;
			}

			protected override int TotalBits => _data.Length * 8;

			protected override int ReadNextByte() => _position < _data.Length ? _data[_position++] : -1;

			public override void Reset()
			{
				_position = 0;
				base.Reset();
			}
		}
	}
}
