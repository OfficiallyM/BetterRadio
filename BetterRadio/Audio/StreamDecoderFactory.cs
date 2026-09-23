using System;
using System.IO;

namespace BetterRadio.Audio
{
	internal static class StreamDecoderFactory
	{
		internal static IStreamDecoder Create(string contentType, Stream stream)
		{
			// Servers often append parameters such as a charset, which are irrelevant for choosing a decoder.
			string type = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

			switch (type)
			{
				case "audio/mpeg":
				case "audio/mp3":
					return new Mp3StreamDecoder(stream);

				default:
					throw new NotSupportedException($"Unsupported stream content type '{contentType}'.");
			}
		}
	}
}
