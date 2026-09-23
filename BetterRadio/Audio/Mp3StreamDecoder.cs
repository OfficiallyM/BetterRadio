using System.IO;
using NLayer;

namespace BetterRadio.Audio
{
	internal class Mp3StreamDecoder : IStreamDecoder
	{
		private readonly MpegFile _mpeg;

		internal Mp3StreamDecoder(Stream stream)
		{
			_mpeg = new MpegFile(stream);
		}

		public int SampleRate => _mpeg.SampleRate;
		public int Channels => _mpeg.Channels;

		public int ReadSamples(float[] buffer, int offset, int count) => _mpeg.ReadSamples(buffer, offset, count);

		public void Dispose()
		{
			_mpeg.Dispose();
		}
	}
}
