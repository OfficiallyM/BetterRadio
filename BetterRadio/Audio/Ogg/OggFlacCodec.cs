using BetterRadio.Audio.Flac;
using BetterRadio.Utilities;
using System;
using System.IO;

namespace BetterRadio.Audio.Ogg
{
	// FLAC in Ogg carries one frame per packet, after a first packet wrapping the STREAMINFO block.
	internal class OggFlacCodec : IOggCodec
	{
		// 0x7F "FLAC", version (2), header count (2), "fLaC", then the 4 byte metadata block header.
		private const int StreamInfoOffset = 17;
		private const int StreamInfoSize = 34;

		private readonly Func<byte[]> _nextPacket;
		private readonly EmitHandler _emit;
		private readonly FlacStreamInfo _streamInfo;
		private readonly FlacFrameDecoder _frameDecoder = new FlacFrameDecoder();
		private readonly FlacBitReader _reader = new FlacBitReader(Stream.Null);

		internal OggFlacCodec(Func<byte[]> nextPacket, EmitHandler emit)
		{
			_nextPacket = nextPacket;
			_emit = emit;

			byte[] head = nextPacket();
			if (head == null || head.Length < StreamInfoOffset + StreamInfoSize)
				throw new InvalidOperationException("Truncated Ogg FLAC header packet.");

			_streamInfo = FlacStreamInfo.Parse(head, StreamInfoOffset);
		}

		public bool DecodeNext()
		{
			byte[] packet = _nextPacket();
			if (packet == null) return false;

			// Anything not starting with a frame sync is a further metadata block, such as Vorbis comments.
			if (packet.Length < 2 || packet[0] != 0xFF)
				return true;

			try
			{
				_reader.SetSource(new MemoryStream(packet, false));
				_frameDecoder.DecodeFrame(_reader, _streamInfo);
			}
			catch (Exception ex) when (ex is InvalidDataException || ex is EndOfStreamException)
			{
				Logging.LogDebug($"Skipped undecodable FLAC packet: {ex.Message}");
				return true;
			}

			_emit(_frameDecoder.Output, 0, _frameDecoder.Frames, _frameDecoder.SampleRate, _frameDecoder.Channels, 0, 1);
			return true;
		}

		public void Dispose()
		{
		}
	}
}
