using BetterRadio.Audio.Aac;
using BetterRadio.Audio.Flac;
using BetterRadio.Audio.Ogg;
using System;

namespace BetterRadio.Audio
{
	internal static class StreamDecoderFactory
	{
		private const int SniffLength = 4;

		// Picks a decoder from the stream's opening bytes where they are distinctive, falling back to the content type.
		// Servers are often vague or wrong about the type (application/octet-stream, audio/flac for Ogg FLAC, and so on).
		internal static IStreamDecoder Create(string contentType, PeekableStream stream)
		{
			switch (Sniff(stream))
			{
				case StreamFormat.Ogg: return new OggStreamDecoder(stream);
				case StreamFormat.Flac: return new FlacStreamDecoder(stream);
				case StreamFormat.Mp3: return new Mp3StreamDecoder(stream);
				case StreamFormat.Aac: return new AdtsStreamDecoder(stream);
			}

			// Servers often append parameters such as a charset, which are irrelevant for choosing a decoder.
			string type = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

			switch (type)
			{
				case "audio/mpeg":
				case "audio/mp3":
				case "audio/mpeg3":
				case "audio/x-mpeg":
					return new Mp3StreamDecoder(stream);

				case "audio/aac":
				case "audio/aacp":
				case "audio/x-aac":
				case "audio/x-aacp":
					return new AdtsStreamDecoder(stream);

				case "audio/ogg":
				case "audio/x-ogg":
				case "application/ogg":
				case "audio/vorbis":
				case "audio/opus":
					return new OggStreamDecoder(stream);

				case "audio/flac":
				case "audio/x-flac":
					return new FlacStreamDecoder(stream);

				default:
					throw new NotSupportedException($"Unsupported stream content type '{contentType}'.");
			}
		}

		private enum StreamFormat
		{
			Unknown,
			Ogg,
			Flac,
			Mp3,
			Aac,
		}

		private static StreamFormat Sniff(PeekableStream stream)
		{
			byte[] start = new byte[SniffLength];
			if (stream.Peek(start, SniffLength) < SniffLength)
				return StreamFormat.Unknown;

			if (start[0] == 'O' && start[1] == 'g' && start[2] == 'g' && start[3] == 'S') return StreamFormat.Ogg;
			if (start[0] == 'f' && start[1] == 'L' && start[2] == 'a' && start[3] == 'C') return StreamFormat.Flac;
			if (start[0] == 'I' && start[1] == 'D' && start[2] == '3') return StreamFormat.Mp3;

			// MP3 and ADTS share a sync word and differ in the layer bits, which ADTS always sets to zero.
			if (start[0] == 0xFF && (start[1] & 0xE0) == 0xE0)
				return AdtsStreamDecoder.IsAdtsSync(start[0], start[1]) ? StreamFormat.Aac : StreamFormat.Mp3;

			return StreamFormat.Unknown;
		}
	}
}
