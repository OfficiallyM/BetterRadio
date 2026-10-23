using System;
using System.IO;

namespace BetterRadio.CustomRadio
{
    public class WavDecoder : IAudioDecoder
    {
        private const short FormatIeeeFloat = 3;

        public bool CanDecode(string path) => path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

        public DecodedAudio Decode(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF")
                    throw new InvalidDataException("Not a RIFF file: " + path);

				// RIFF chunk size, unused.
				reader.ReadInt32(); 

                if (new string(reader.ReadChars(4)) != "WAVE")
                    throw new InvalidDataException("Not a WAVE file: " + path);

                short formatTag = 0;
                short channels = 0;
                int sampleRate = 0;
                short bitsPerSample = 0;
                float[] samples = null;

                while (stream.Position < stream.Length)
                {
                    string chunkId = new string(reader.ReadChars(4));
                    int chunkSize = reader.ReadInt32();
                    long chunkEnd = stream.Position + chunkSize;

                    if (chunkId == "fmt ")
                    {
                        formatTag = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        sampleRate = reader.ReadInt32();
						// Byte rate, unused.
						reader.ReadInt32();
						// Block align, unused.
						reader.ReadInt16();
                        bitsPerSample = reader.ReadInt16();
                    }
                    else if (chunkId == "data")
                    {
                        samples = ReadSamples(reader, chunkSize, formatTag, bitsPerSample);
                    }

                    // Chunks are word-aligned - skip any padding byte along with whatever we didn't read.
                    stream.Position = chunkEnd + (chunkEnd % 2);
                }

                if (samples == null)
                    throw new InvalidDataException("No data chunk found: " + path);

                return new DecodedAudio
                {
                    Samples = samples,
                    Channels = channels,
                    SampleRate = sampleRate
                };
            }
        }

        private static float[] ReadSamples(BinaryReader reader, int chunkSize, short formatTag, short bitsPerSample)
        {
            switch (bitsPerSample)
            {
                case 8:
                    return ReadPcm8(reader, chunkSize);
                case 16:
                    return ReadPcm16(reader, chunkSize);
                case 24:
                    return ReadPcm24(reader, chunkSize);
                case 32:
                    return formatTag == FormatIeeeFloat
                        ? ReadFloat32(reader, chunkSize)
                        : ReadPcm32(reader, chunkSize);
                default:
                    throw new NotSupportedException("Unsupported WAV bit depth: " + bitsPerSample);
            }
        }

        private static float[] ReadPcm8(BinaryReader reader, int chunkSize)
        {
            var samples = new float[chunkSize];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (reader.ReadByte() - 128) / 128f;
            return samples;
        }

        private static float[] ReadPcm16(BinaryReader reader, int chunkSize)
        {
            var samples = new float[chunkSize / 2];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = reader.ReadInt16() / 32768f;
            return samples;
        }

        private static float[] ReadPcm24(BinaryReader reader, int chunkSize)
        {
            var samples = new float[chunkSize / 3];
            for (int i = 0; i < samples.Length; i++)
            {
                int b0 = reader.ReadByte();
                int b1 = reader.ReadByte();
                int b2 = reader.ReadByte();
                int value = b0 | (b1 << 8) | (b2 << 16);

                // Sign-extend the 24-bit value into a 32-bit int.
                if ((value & 0x800000) != 0)
                    value |= unchecked((int)0xFF000000);

                samples[i] = value / 8388608f;
            }
            return samples;
        }

        private static float[] ReadPcm32(BinaryReader reader, int chunkSize)
        {
            var samples = new float[chunkSize / 4];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = reader.ReadInt32() / 2147483648f;
            return samples;
        }

        private static float[] ReadFloat32(BinaryReader reader, int chunkSize)
        {
            var samples = new float[chunkSize / 4];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = reader.ReadSingle();
            return samples;
        }
    }
}
