using BetterRadio.Utilities;
using System;
using System.IO;

namespace BetterRadio.Audio.Flac
{
	// Decodes native FLAC (as opposed to FLAC inside Ogg). Frames are not length-delimited, so they are decoded
	// straight off the stream, and after a corrupt frame the stream is scanned for the next valid frame header.
	internal class FlacStreamDecoder : BufferedStreamDecoder
	{
		private const int MaxConsecutiveErrors = 50;
		private const int MetadataHeaderSize = 4;
		private const int StreamInfoType = 0;
		private const int StreamInfoSize = 34;

		private readonly PeekableStream _stream;
		private readonly FlacBitReader _reader;
		private readonly FlacFrameDecoder _frameDecoder = new FlacFrameDecoder();
		private FlacStreamInfo _streamInfo;
		private bool _readMetadata;
		private int _consecutiveErrors;

		internal FlacStreamDecoder(PeekableStream stream)
		{
			_stream = stream;
			_reader = new FlacBitReader(stream);
		}

		protected override bool DecodeNextBlock()
		{
			if (!_readMetadata)
			{
				_readMetadata = true;
				if (!ReadMetadata())
					return false;
			}

			if (!FindFrameSync())
				return false;

			try
			{
				_frameDecoder.DecodeFrame(_reader, _streamInfo);
				_consecutiveErrors = 0;
			}
			catch (EndOfStreamException)
			{
				return false;
			}
			catch (InvalidDataException ex)
			{
				if (++_consecutiveErrors >= MaxConsecutiveErrors)
					throw new InvalidOperationException($"FLAC stream could not be decoded: {ex.Message}", ex);

				Logging.LogDebug($"Skipped undecodable FLAC frame: {ex.Message}");

				// Drop any half-read bits so the resync scan starts on a byte boundary.
				_reader.SetSource(_stream);
				return true;
			}

			Emit(_frameDecoder.Output, 0, _frameDecoder.Frames, _frameDecoder.SampleRate, _frameDecoder.Channels);
			return true;
		}

		// Reads the "fLaC" marker and metadata blocks, keeping STREAMINFO. A stream joined partway through has neither,
		// in which case decoding relies on frame headers that spell out their own format.
		private bool ReadMetadata()
		{
			byte[] marker = new byte[4];
			if (_stream.Peek(marker, 4) < 4) return false;
			if (marker[0] != 'f' || marker[1] != 'L' || marker[2] != 'a' || marker[3] != 'C') return true;

			_stream.Skip(4);

			byte[] header = new byte[MetadataHeaderSize];
			bool last = false;
			while (!last)
			{
				if (!_stream.TryReadFully(header, 0, MetadataHeaderSize)) return false;

				last = (header[0] & 0x80) != 0;
				int type = header[0] & 0x7F;
				int length = (header[1] << 16) | (header[2] << 8) | header[3];

				if (type == StreamInfoType && length >= StreamInfoSize)
				{
					byte[] body = new byte[length];
					if (!_stream.TryReadFully(body, 0, length)) return false;

					_streamInfo = FlacStreamInfo.Parse(body, 0);
				}
				else if (!_stream.Skip(length))
				{
					return false;
				}
			}

			return true;
		}

		// Frames start with 0xFFF8 or 0xFFF9. Normally the stream is already there; after an error this skips forward.
		private bool FindFrameSync()
		{
			while (true)
			{
				if (!_stream.TryPeek(0, out byte first) || !_stream.TryPeek(1, out byte second))
					return false;

				if (first == 0xFF && (second & 0xFE) == 0xF8)
					return true;

				_stream.Skip(1);
			}
		}
	}
}
