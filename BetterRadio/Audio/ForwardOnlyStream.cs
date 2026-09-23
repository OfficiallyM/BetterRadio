using System;
using System.IO;

namespace BetterRadio.Audio
{
	// NLayer's reader asks for Position even on streams that cannot seek, and the raw network stream throws when asked, so this reports how many bytes have been consumed instead.
	internal class ForwardOnlyStream : Stream
	{
		private readonly Stream _inner;
		private long _position;

		internal ForwardOnlyStream(Stream inner)
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

		public override int Read(byte[] buffer, int offset, int count)
		{
			int read = _inner.Read(buffer, offset, count);
			_position += read;
			return read;
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
