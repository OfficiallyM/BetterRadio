using System;
using System.IO;

namespace BetterRadio.Audio
{
	// Wraps a forward-only network stream so the start of the data can be sniffed before a decoder is chosen, and so
	// decoders can look ahead for the next frame's sync word without consuming it.
	// NLayer's reader also asks for Position even on streams that cannot seek, so this reports how many bytes have been consumed.
	internal class PeekableStream : Stream
	{
		private const int MinimumBufferSize = 8192;

		private readonly Stream _inner;
		private byte[] _buffer = new byte[MinimumBufferSize];
		private int _start;
		private int _end;
		private bool _innerEnded;
		private long _position;

		internal PeekableStream(Stream inner)
		{
			_inner = inner;
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => _position;
			set => throw new NotSupportedException();
		}

		// Returns false when the stream ends before offset is reached.
		internal bool TryPeek(int offset, out byte value)
		{
			if (!Fill(offset + 1))
			{
				value = 0;
				return false;
			}

			value = _buffer[_start + offset];
			return true;
		}

		// Copies up to count upcoming bytes without consuming them, returning how many were available.
		internal int Peek(byte[] destination, int count)
		{
			Fill(count);
			int available = Math.Min(count, _end - _start);
			Array.Copy(_buffer, _start, destination, 0, available);
			return available;
		}

		public override int ReadByte()
		{
			if (_start == _end && !Fill(1)) return -1;

			_position++;
			return _buffer[_start++];
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count == 0) return 0;

			// Peeked bytes are served first; only once they are gone does a read go straight to the network.
			if (_start == _end)
				return ReadInner(buffer, offset, count);

			int read = Math.Min(count, _end - _start);
			Array.Copy(_buffer, _start, buffer, offset, read);
			_start += read;
			_position += read;
			return read;
		}

		// Reads exactly count bytes, returning false if the stream ended first.
		internal bool TryReadFully(byte[] buffer, int offset, int count)
		{
			while (count > 0)
			{
				int read = Read(buffer, offset, count);
				if (read <= 0) return false;

				offset += read;
				count -= read;
			}

			return true;
		}

		internal bool Skip(int count)
		{
			while (count > 0)
			{
				if (_start == _end && !Fill(1)) return false;

				int skipped = Math.Min(count, _end - _start);
				_start += skipped;
				_position += skipped;
				count -= skipped;
			}

			return true;
		}

		private int ReadInner(byte[] buffer, int offset, int count)
		{
			if (_innerEnded) return 0;

			int read = _inner.Read(buffer, offset, count);
			if (read <= 0)
			{
				_innerEnded = true;
				return 0;
			}

			_position += read;
			return read;
		}

		// Makes sure at least count bytes are buffered, returning false if the stream ended first.
		private bool Fill(int count)
		{
			while (_end - _start < count)
			{
				if (_innerEnded) return false;

				if (_buffer.Length - _start < count)
				{
					// Not enough room after the read position, so slide the unread bytes to the front, growing if needed.
					byte[] target = _buffer.Length < count ? new byte[Math.Max(count, _buffer.Length * 2)] : _buffer;
					Array.Copy(_buffer, _start, target, 0, _end - _start);
					_end -= _start;
					_start = 0;
					_buffer = target;
				}

				int read = _inner.Read(_buffer, _end, _buffer.Length - _end);
				if (read <= 0)
				{
					_innerEnded = true;
					return false;
				}

				_end += read;
			}

			return true;
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_inner.Dispose();

			base.Dispose(disposing);
		}
	}
}
