using System;
using NLayer;

namespace BetterRadio.CustomRadio
{
    public class Mp3Decoder : IAudioDecoder
    {
        public bool CanDecode(string path) => path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);

        public DecodedAudio Decode(string path)
        {
            using (var mpegFile = new MpegFile(path))
            {
                // Length is the total interleaved PCM sample count for the whole file.
                var samples = new float[(int)mpegFile.Length];
                int read = mpegFile.ReadSamples(samples, 0, samples.Length);

                if (read != samples.Length)
                    Array.Resize(ref samples, read);

                return new DecodedAudio
                {
                    Samples = samples,
                    Channels = mpegFile.Channels,
                    SampleRate = mpegFile.SampleRate
                };
            }
        }
    }
}
