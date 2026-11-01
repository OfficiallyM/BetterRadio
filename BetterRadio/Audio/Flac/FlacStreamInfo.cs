namespace BetterRadio.Audio.Flac
{
	// The parts of a FLAC STREAMINFO block a frame header can defer to.
	internal class FlacStreamInfo
	{
		internal int SampleRate;
		internal int Channels;
		internal int BitsPerSample;

		// Parses the 34 byte STREAMINFO body, with the metadata block header already stripped.
		internal static FlacStreamInfo Parse(byte[] data, int offset)
		{
			// Bytes 10 to 13 hold 20 bits of sample rate, 3 bits of channels minus one, then the top bit of bits per sample minus one.
			int sampleRate = (data[offset + 10] << 12) | (data[offset + 11] << 4) | (data[offset + 12] >> 4);
			int channels = ((data[offset + 12] >> 1) & 0x07) + 1;
			int bitsPerSample = (((data[offset + 12] & 0x01) << 4) | (data[offset + 13] >> 4)) + 1;

			return new FlacStreamInfo
			{
				SampleRate = sampleRate,
				Channels = channels,
				BitsPerSample = bitsPerSample,
			};
		}
	}
}
