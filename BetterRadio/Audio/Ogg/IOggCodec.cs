using System;

namespace BetterRadio.Audio.Ogg
{
	internal delegate void EmitHandler(float[] samples, int offset, int frames, int sampleRate, int channels, int leftIndex, int rightIndex);

	// Decodes one logical Ogg stream, pulling its packets one at a time.
	internal interface IOggCodec : IDisposable
	{
		// Decodes the next packet or few and emits the audio. Returns false once the logical stream has ended.
		bool DecodeNext();
	}

	internal static class OggChannelOrder
	{
		// Vorbis and Opus share the Vorbis channel order, where three channel and 5.x layouts put centre between left and right.
		internal static int RightIndex(int channels) => channels == 3 || channels >= 5 ? 2 : 1;
	}
}
