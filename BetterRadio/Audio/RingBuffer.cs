using System;

namespace BetterRadio.Audio
{
	internal class RingBuffer
	{
		private readonly float[] _data;
		private readonly int _channels;
		private readonly object _lock = new object();
		private int _readPosition;
		private int _count;

		internal RingBuffer(int capacity, int channels)
		{
			_channels = channels;

			// Rounded down so the capacity is always a whole number of frames.
			_data = new float[capacity - (capacity % channels)];
		}

		internal int Available
		{
			get
			{
				lock (_lock)
					return _count;
			}
		}

		// Returns how many samples were written, which is less than requested when the buffer is full.
		internal int Write(float[] source, int offset, int count)
		{
			lock (_lock)
			{
				int toWrite = Math.Min(count, _data.Length - _count);
				int writePosition = (_readPosition + _count) % _data.Length;
				int firstPart = Math.Min(toWrite, _data.Length - writePosition);

				Array.Copy(source, offset, _data, writePosition, firstPart);
				Array.Copy(source, offset + firstPart, _data, 0, toWrite - firstPart);

				_count += toWrite;
				return toWrite;
			}
		}

		// Returns how many samples were read, which is less than requested on an underrun.
		internal int Read(float[] destination, int offset, int count)
		{
			lock (_lock)
			{
				int toRead = Math.Min(count, _count);

				// Only whole frames are handed out so an underrun can never leave the channels swapped.
				toRead -= toRead % _channels;

				int firstPart = Math.Min(toRead, _data.Length - _readPosition);

				Array.Copy(_data, _readPosition, destination, offset, firstPart);
				Array.Copy(_data, 0, destination, offset + firstPart, toRead - firstPart);

				_readPosition = (_readPosition + toRead) % _data.Length;
				_count -= toRead;
				return toRead;
			}
		}
	}
}
