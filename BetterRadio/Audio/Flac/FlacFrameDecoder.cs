using System;
using System.IO;

namespace BetterRadio.Audio.Flac
{
	// Decodes single FLAC frames into interleaved floats. Shared by native FLAC streams and FLAC inside Ogg.
	internal class FlacFrameDecoder
	{
		private const int SyncCode = 0x3FFE;

		private static readonly int[] SampleRateCodes = { 0, 88200, 176400, 192000, 8000, 16000, 22050, 24000, 32000, 44100, 48000, 96000 };
		private static readonly int[] BitsPerSampleCodes = { 0, 8, 12, 0, 16, 20, 24, 32 };
		private static readonly byte[] Crc8Table = BuildCrc8Table();

		private long[][] _channelSamples = new long[0][];
		private long[] _lpcCoefficients = new long[32];

		internal float[] Output { get; private set; } = new float[0];
		internal int Frames { get; private set; }
		internal int SampleRate { get; private set; }
		internal int Channels { get; private set; }

		// Reads and decodes one frame, starting at its sync code. Throws InvalidDataException on a bad or unsupported frame,
		// after which the reader's position is undefined and the caller needs to resync.
		internal void DecodeFrame(FlacBitReader reader, FlacStreamInfo streamInfo)
		{
			byte crc = 0;
			uint ReadHeaderByte()
			{
				uint value = reader.ReadBits(8);
				crc = Crc8Table[crc ^ value];
				return value;
			}

			uint b0 = ReadHeaderByte();
			uint b1 = ReadHeaderByte();
			if (((b0 << 6) | (b1 >> 2)) != SyncCode)
				throw new InvalidDataException("Missing FLAC frame sync.");

			uint b2 = ReadHeaderByte();
			uint b3 = ReadHeaderByte();
			int blockSizeCode = (int)(b2 >> 4);
			int sampleRateCode = (int)(b2 & 0x0F);
			int channelAssignment = (int)(b3 >> 4);
			int bitsPerSampleCode = (int)((b3 >> 1) & 0x07);

			if ((b1 & 0x02) != 0 || (b3 & 0x01) != 0 || blockSizeCode == 0 || sampleRateCode == 15 || channelAssignment > 10 || bitsPerSampleCode == 3)
				throw new InvalidDataException("Invalid FLAC frame header.");

			// The frame or sample number is UTF-8 style coded. Only its length matters here.
			uint lead = ReadHeaderByte();
			int extraBytes = 0;
			while (extraBytes < 7 && (lead & (0x80 >> extraBytes)) != 0)
				extraBytes++;

			if (extraBytes == 1 || extraBytes > 7)
				throw new InvalidDataException("Invalid FLAC frame number.");

			for (int i = 1; i < extraBytes; i++)
			{
				if ((ReadHeaderByte() & 0xC0) != 0x80)
					throw new InvalidDataException("Invalid FLAC frame number.");
			}

			int blockSize;
			if (blockSizeCode == 1) blockSize = 192;
			else if (blockSizeCode <= 5) blockSize = 576 << (blockSizeCode - 2);
			else if (blockSizeCode == 6) blockSize = (int)ReadHeaderByte() + 1;
			else if (blockSizeCode == 7) blockSize = (int)((ReadHeaderByte() << 8) | ReadHeaderByte()) + 1;
			else blockSize = 256 << (blockSizeCode - 8);

			int sampleRate;
			if (sampleRateCode == 0) sampleRate = streamInfo?.SampleRate ?? 0;
			else if (sampleRateCode < 12) sampleRate = SampleRateCodes[sampleRateCode];
			else if (sampleRateCode == 12) sampleRate = (int)ReadHeaderByte() * 1000;
			else if (sampleRateCode == 13) sampleRate = (int)((ReadHeaderByte() << 8) | ReadHeaderByte());
			else sampleRate = (int)((ReadHeaderByte() << 8) | ReadHeaderByte()) * 10;

			int bitsPerSample = bitsPerSampleCode == 0 ? streamInfo?.BitsPerSample ?? 0 : BitsPerSampleCodes[bitsPerSampleCode];

			byte expectedCrc = crc;
			if (reader.ReadBits(8) != expectedCrc)
				throw new InvalidDataException("FLAC frame header CRC mismatch.");

			// A frame that defers to STREAMINFO is undecodable if the stream was joined without one.
			if (sampleRate <= 0 || bitsPerSample <= 0)
				throw new InvalidDataException("FLAC frame needs STREAMINFO, which has not been seen.");

			int channels = channelAssignment < 8 ? channelAssignment + 1 : 2;
			EnsureCapacity(channels, blockSize);

			for (int channel = 0; channel < channels; channel++)
			{
				// The side channel carries one extra bit, since it is a difference of two full range channels.
				bool isSide = (channelAssignment == 8 && channel == 1) || (channelAssignment == 9 && channel == 0) || (channelAssignment == 10 && channel == 1);
				DecodeSubframe(reader, _channelSamples[channel], blockSize, bitsPerSample + (isSide ? 1 : 0));
			}

			Decorrelate(channelAssignment, blockSize);

			// The footer is padding to a byte boundary then a CRC-16 over the whole frame, which is not verified;
			// the header CRC already guards against false syncs and a corrupt body just sounds like a glitch.
			reader.AlignToByte();
			reader.ReadBits(16);

			WriteOutput(channels, blockSize, bitsPerSample);
			SampleRate = sampleRate;
			Channels = channels;
		}

		private void DecodeSubframe(FlacBitReader reader, long[] samples, int blockSize, int bitsPerSample)
		{
			if (reader.ReadBit())
				throw new InvalidDataException("Invalid FLAC subframe padding.");

			int type = (int)reader.ReadBits(6);

			int wastedBits = 0;
			if (reader.ReadBit())
				wastedBits = reader.ReadUnary() + 1;

			bitsPerSample -= wastedBits;
			if (bitsPerSample <= 0)
				throw new InvalidDataException("Invalid FLAC wasted bits.");

			if (type == 0)
			{
				long value = reader.ReadSignedLong(bitsPerSample);
				for (int i = 0; i < blockSize; i++)
					samples[i] = value;
			}
			else if (type == 1)
			{
				for (int i = 0; i < blockSize; i++)
					samples[i] = reader.ReadSignedLong(bitsPerSample);
			}
			else if (type >= 8 && type <= 12)
			{
				DecodeFixed(reader, samples, blockSize, bitsPerSample, type - 8);
			}
			else if (type >= 32)
			{
				DecodeLpc(reader, samples, blockSize, bitsPerSample, type - 31);
			}
			else
			{
				throw new InvalidDataException($"Reserved FLAC subframe type {type}.");
			}

			if (wastedBits > 0)
			{
				for (int i = 0; i < blockSize; i++)
					samples[i] <<= wastedBits;
			}
		}

		private static void DecodeFixed(FlacBitReader reader, long[] samples, int blockSize, int bitsPerSample, int order)
		{
			if (order > blockSize)
				throw new InvalidDataException("FLAC predictor order exceeds block size.");

			for (int i = 0; i < order; i++)
				samples[i] = reader.ReadSignedLong(bitsPerSample);

			DecodeResidual(reader, samples, blockSize, order);

			switch (order)
			{
				case 1:
					for (int i = 1; i < blockSize; i++)
						samples[i] += samples[i - 1];
					break;
				case 2:
					for (int i = 2; i < blockSize; i++)
						samples[i] += 2 * samples[i - 1] - samples[i - 2];
					break;
				case 3:
					for (int i = 3; i < blockSize; i++)
						samples[i] += 3 * samples[i - 1] - 3 * samples[i - 2] + samples[i - 3];
					break;
				case 4:
					for (int i = 4; i < blockSize; i++)
						samples[i] += 4 * samples[i - 1] - 6 * samples[i - 2] + 4 * samples[i - 3] - samples[i - 4];
					break;
			}
		}

		private void DecodeLpc(FlacBitReader reader, long[] samples, int blockSize, int bitsPerSample, int order)
		{
			if (order > blockSize)
				throw new InvalidDataException("FLAC predictor order exceeds block size.");

			for (int i = 0; i < order; i++)
				samples[i] = reader.ReadSignedLong(bitsPerSample);

			int precision = (int)reader.ReadBits(4) + 1;
			if (precision == 16)
				throw new InvalidDataException("Invalid FLAC LPC precision.");

			int shift = reader.ReadSigned(5);
			if (shift < 0)
				throw new InvalidDataException("Negative FLAC LPC shift.");

			for (int i = 0; i < order; i++)
				_lpcCoefficients[i] = reader.ReadSigned(precision);

			DecodeResidual(reader, samples, blockSize, order);

			for (int i = order; i < blockSize; i++)
			{
				long prediction = 0;
				for (int j = 0; j < order; j++)
					prediction += _lpcCoefficients[j] * samples[i - 1 - j];

				samples[i] += prediction >> shift;
			}
		}

		// Residuals are written straight into samples after the warm-up, then the predictor adds onto them in place.
		private static void DecodeResidual(FlacBitReader reader, long[] samples, int blockSize, int order)
		{
			int method = (int)reader.ReadBits(2);
			if (method > 1)
				throw new InvalidDataException("Reserved FLAC residual coding method.");

			int parameterBits = method == 0 ? 4 : 5;
			int escapeCode = (1 << parameterBits) - 1;
			int partitionOrder = (int)reader.ReadBits(4);
			int partitions = 1 << partitionOrder;
			int partitionSize = blockSize >> partitionOrder;

			if (partitionSize << partitionOrder != blockSize || partitionSize < order)
				throw new InvalidDataException("Invalid FLAC residual partitioning.");

			int index = order;
			for (int partition = 0; partition < partitions; partition++)
			{
				int count = partition == 0 ? partitionSize - order : partitionSize;
				int parameter = (int)reader.ReadBits(parameterBits);

				if (parameter == escapeCode)
				{
					int rawBits = (int)reader.ReadBits(5);
					for (int i = 0; i < count; i++)
						samples[index++] = reader.ReadSigned(rawBits);
				}
				else
				{
					for (int i = 0; i < count; i++)
						samples[index++] = reader.ReadRice(parameter);
				}
			}
		}

		private void Decorrelate(int channelAssignment, int blockSize)
		{
			if (channelAssignment < 8) return;

			long[] first = _channelSamples[0];
			long[] second = _channelSamples[1];

			for (int i = 0; i < blockSize; i++)
			{
				switch (channelAssignment)
				{
					// Left and side: right = left - side.
					case 8:
						second[i] = first[i] - second[i];
						break;

					// Side and right: left = side + right.
					case 9:
						first[i] += second[i];
						break;

					// Mid and side, where mid lost its low bit to the halving, which the side's low bit restores.
					default:
						long mid = (first[i] << 1) | (second[i] & 1);
						long side = second[i];
						first[i] = (mid + side) >> 1;
						second[i] = (mid - side) >> 1;
						break;
				}
			}
		}

		private void WriteOutput(int channels, int blockSize, int bitsPerSample)
		{
			int total = channels * blockSize;
			if (Output.Length < total)
				Output = new float[total];

			float scale = 1f / (1L << (bitsPerSample - 1));
			float[] output = Output;

			for (int channel = 0; channel < channels; channel++)
			{
				long[] samples = _channelSamples[channel];
				for (int i = 0, o = channel; i < blockSize; i++, o += channels)
					output[o] = samples[i] * scale;
			}

			Frames = blockSize;
		}

		private void EnsureCapacity(int channels, int blockSize)
		{
			if (_channelSamples.Length < channels)
				Array.Resize(ref _channelSamples, channels);

			for (int i = 0; i < channels; i++)
			{
				if (_channelSamples[i] == null || _channelSamples[i].Length < blockSize)
					_channelSamples[i] = new long[blockSize];
			}
		}

		private static byte[] BuildCrc8Table()
		{
			byte[] table = new byte[256];
			for (int i = 0; i < 256; i++)
			{
				int crc = i;
				for (int bit = 0; bit < 8; bit++)
					crc = (crc & 0x80) != 0 ? (crc << 1) ^ 0x07 : crc << 1;

				table[i] = (byte)crc;
			}

			return table;
		}
	}
}
