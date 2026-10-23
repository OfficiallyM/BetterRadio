namespace BetterRadio.CustomRadio
{
    // Result of decoding a custom radio track to raw interleaved PCM data.
    public struct DecodedAudio
    {
        public float[] Samples;
        public int Channels;
        public int SampleRate;
    }
}
