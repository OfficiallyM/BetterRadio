using System;

namespace BetterRadio.Audio
{
	internal interface IStreamDecoder : IDisposable
	{
		// Both are only valid once the first samples have been read.
		int SampleRate { get; }
		int Channels { get; }

		// Returns 0 once the stream has ended.
		int ReadSamples(float[] buffer, int offset, int count);
	}
}
