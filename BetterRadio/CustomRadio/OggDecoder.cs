using System;
using System.Collections.Generic;
using NVorbis;

namespace BetterRadio.CustomRadio
{
    public class OggDecoder : IAudioDecoder
    {
        public bool CanDecode(string path) => path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);

        public DecodedAudio Decode(string path)
        {
            using (var vorbis = new VorbisReader(path))
            {
                int channels = vorbis.Channels;
                int sampleRate = vorbis.SampleRate;

                // Vorbis doesn't expose an up-front sample count, so read in chunks and collect them.
                var buffer = new float[channels * sampleRate];
                var samples = new List<float>((int)(vorbis.TotalTime.TotalSeconds * sampleRate * channels));

                int read;
                while ((read = vorbis.ReadSamples(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                        samples.Add(buffer[i]);
                }

                return new DecodedAudio
                {
                    Samples = samples.ToArray(),
                    Channels = channels,
                    SampleRate = sampleRate
                };
            }
        }
    }
}
