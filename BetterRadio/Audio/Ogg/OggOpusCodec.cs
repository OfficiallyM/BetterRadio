using BetterRadio.Utilities;
using Concentus;
using System;
using System.Text;

namespace BetterRadio.Audio.Ogg
{
	internal class OggOpusCodec : IOggCodec
	{
		// Opus always decodes at 48 kHz, and a single packet holds at most 120 ms.
		private const int SampleRate = 48000;
		private const int MaxFrameSize = 5760;

		private readonly Func<byte[]> _nextPacket;
		private readonly EmitHandler _emit;
		private readonly IOpusDecoder _decoder;
		private readonly IOpusMultiStreamDecoder _multiStreamDecoder;
		private readonly int _channels;
		private readonly float _gain;
		private readonly float[] _pcm;
		private int _preSkip;

		internal OggOpusCodec(Func<byte[]> nextPacket, EmitHandler emit)
		{
			_nextPacket = nextPacket;
			_emit = emit;

			byte[] head = nextPacket();
			if (head == null || head.Length < 19)
				throw new InvalidOperationException("Truncated OpusHead packet.");

			_channels = head[9];
			_preSkip = BitConverter.ToUInt16(head, 10);
			_gain = (float)Math.Pow(10, BitConverter.ToInt16(head, 16) / (20.0 * 256.0));
			int mappingFamily = head[18];

			// Concentus would otherwise go looking for a native libopus, which a Unity mod has no business loading.
			OpusCodecFactory.AttemptToUseNativeLibrary = false;

			if (mappingFamily == 0)
			{
				_decoder = OpusCodecFactory.CreateDecoder(SampleRate, _channels);
			}
			else
			{
				if (head.Length < 21 + _channels)
					throw new InvalidOperationException("Truncated OpusHead channel mapping.");

				byte[] mapping = new byte[_channels];
				Array.Copy(head, 21, mapping, 0, _channels);
				_multiStreamDecoder = OpusCodecFactory.CreateMultiStreamDecoder(SampleRate, _channels, head[19], head[20], mapping);
			}

			_pcm = new float[MaxFrameSize * _channels];
		}

		public bool DecodeNext()
		{
			byte[] packet = _nextPacket();
			if (packet == null) return false;

			if (packet.Length >= 8 && Encoding.ASCII.GetString(packet, 0, 8) == "OpusTags")
				return true;

			int frames;
			try
			{
				frames = _decoder != null
					? _decoder.Decode(packet, _pcm, MaxFrameSize)
					: _multiStreamDecoder.DecodeMultistream(packet, _pcm, MaxFrameSize, false);
			}
			catch (Exception ex)
			{
				// A corrupt packet is dropped; Opus recovers on the next one.
				Logging.LogDebug($"Skipped undecodable Opus packet: {ex.Message}");
				return true;
			}

			// The encoder's start-up delay is trimmed from the front of each logical stream.
			int skip = Math.Min(_preSkip, frames);
			_preSkip -= skip;

			if (_gain != 1f)
			{
				for (int i = skip * _channels; i < frames * _channels; i++)
					_pcm[i] *= _gain;
			}

			_emit(_pcm, skip * _channels, frames - skip, SampleRate, _channels, 0, OggChannelOrder.RightIndex(_channels));
			return true;
		}

		public void Dispose()
		{
		}
	}
}
