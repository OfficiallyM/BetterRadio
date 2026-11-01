using System.IO;

namespace BetterRadio.Audio.Flac
{
	// MSB-first bit reader. Bytes are only pulled from the source as they are needed, so after a frame the source is
	// left positioned exactly at the start of the next one, which matters for native FLAC where frames are not delimited.
	internal class FlacBitReader
	{
		private Stream _source;
		private ulong _bits;
		private int _bitCount;

		internal FlacBitReader(Stream source)
		{
			_source = source;
		}

		internal bool IsByteAligned => _bitCount % 8 == 0;

		internal void SetSource(Stream source)
		{
			_source = source;
			_bits = 0;
			_bitCount = 0;
		}

		// Unsigned read of up to 32 bits.
		internal uint ReadBits(int count)
		{
			if (count == 0) return 0;

			Need(count);
			_bitCount -= count;
			return (uint)((_bits >> _bitCount) & ((1UL << count) - 1));
		}

		// Two's complement read of up to 32 bits, sign extended.
		internal int ReadSigned(int count)
		{
			if (count == 0) return 0;

			uint value = ReadBits(count);
			int shift = 32 - count;
			return (int)(value << shift) >> shift;
		}

		// Two's complement read of up to 33 bits, which a side channel of 32 bit audio needs.
		internal long ReadSignedLong(int count)
		{
			if (count <= 32) return ReadSigned(count);

			long high = ReadSigned(count - 32);
			return (high << 32) | ReadBits(32);
		}

		internal bool ReadBit() => ReadBits(1) != 0;

		// Counts zero bits up to and including the terminating one bit.
		internal int ReadUnary()
		{
			int count = 0;
			while (true)
			{
				if (_bitCount == 0)
					Need(8);

				// Fast path for a whole run of buffered zeros.
				ulong mask = (1UL << _bitCount) - 1;
				ulong remaining = _bits & mask;
				if (remaining == 0)
				{
					count += _bitCount;
					_bitCount = 0;
					continue;
				}

				int highest = 63;
				while ((remaining >> highest) == 0)
					highest--;

				count += _bitCount - 1 - highest;
				_bitCount = highest;
				return count;
			}
		}

		internal int ReadRice(int parameter)
		{
			uint quotient = (uint)ReadUnary();
			uint value = (quotient << parameter) | ReadBits(parameter);

			// Zigzag decode: even values are positive, odd values negative.
			return (int)(value >> 1) ^ -(int)(value & 1);
		}

		internal void AlignToByte()
		{
			_bitCount -= _bitCount % 8;
		}

		private void Need(int count)
		{
			while (_bitCount < count)
			{
				int next = _source.ReadByte();
				if (next < 0)
					throw new EndOfStreamException();

				_bits = (_bits << 8) | (uint)next;
				_bitCount += 8;
			}
		}
	}
}
