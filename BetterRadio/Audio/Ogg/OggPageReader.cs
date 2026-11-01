using System;
using System.Collections.Generic;

namespace BetterRadio.Audio.Ogg
{
	internal class OggPage
	{
		internal int Serial;
		internal bool IsContinuation;
		internal bool IsBeginningOfStream;
		internal bool IsEndOfStream;

		// Packets that complete on this page. The first may be the tail of a packet started on an earlier page,
		// which IsContinuation flags, and the last may be unfinished, which LastPacketIsPartial flags.
		internal readonly List<byte[]> Packets = new List<byte[]>();
		internal bool LastPacketIsPartial;
	}

	// Splits an Ogg bitstream into pages, resyncing on the "OggS" capture pattern if the stream is joined partway
	// through a page or a page is corrupt.
	internal class OggPageReader
	{
		private const int HeaderSize = 27;
		private const int MaxPageSize = HeaderSize + 255 + 255 * 255;

		private readonly PeekableStream _stream;
		private readonly byte[] _header = new byte[HeaderSize];
		private readonly byte[] _segments = new byte[255];
		private readonly byte[] _page = new byte[MaxPageSize];

		internal OggPageReader(PeekableStream stream)
		{
			_stream = stream;
		}

		// Returns null once the stream has ended.
		internal OggPage ReadPage()
		{
			while (true)
			{
				if (!FindCapturePattern()) return null;
				if (_stream.Peek(_header, HeaderSize) < HeaderSize) return null;

				int segmentCount = _header[26];
				if (_header[4] != 0 || !_stream.TryPeek(HeaderSize + segmentCount - 1, out _))
				{
					if (!_stream.Skip(1)) return null;
					continue;
				}

				// Peeking the segment table means a false capture pattern only costs one byte of resync.
				for (int i = 0; i < segmentCount; i++)
				{
					_stream.TryPeek(HeaderSize + i, out byte lacing);
					_segments[i] = lacing;
				}

				int bodySize = 0;
				for (int i = 0; i < segmentCount; i++)
					bodySize += _segments[i];

				int pageSize = HeaderSize + segmentCount + bodySize;
				if (_stream.Peek(_page, pageSize) < pageSize) return null;

				if (!HasValidChecksum(pageSize))
				{
					_stream.Skip(1);
					continue;
				}

				_stream.Skip(pageSize);
				return BuildPage(segmentCount, HeaderSize + segmentCount);
			}
		}

		private bool FindCapturePattern()
		{
			while (true)
			{
				if (!_stream.TryPeek(3, out byte fourth)) return false;

				_stream.TryPeek(0, out byte first);
				_stream.TryPeek(1, out byte second);
				_stream.TryPeek(2, out byte third);

				if (first == 'O' && second == 'g' && third == 'g' && fourth == 'S')
					return true;

				_stream.Skip(1);
			}
		}

		private OggPage BuildPage(int segmentCount, int bodyStart)
		{
			byte flags = _header[5];
			OggPage page = new OggPage
			{
				Serial = BitConverter.ToInt32(_header, 14),
				IsContinuation = (flags & 0x01) != 0,
				IsBeginningOfStream = (flags & 0x02) != 0,
				IsEndOfStream = (flags & 0x04) != 0,
			};

			// A lacing value of 255 means the packet carries on into the next segment; anything less ends it.
			int position = bodyStart;
			int packetStart = bodyStart;
			for (int i = 0; i < segmentCount; i++)
			{
				position += _segments[i];
				if (_segments[i] < 255)
				{
					page.Packets.Add(Slice(packetStart, position));
					packetStart = position;
				}
			}

			if (segmentCount > 0 && _segments[segmentCount - 1] == 255)
			{
				page.Packets.Add(Slice(packetStart, position));
				page.LastPacketIsPartial = true;
			}

			return page;
		}

		private byte[] Slice(int start, int end)
		{
			byte[] data = new byte[end - start];
			Array.Copy(_page, start, data, 0, data.Length);
			return data;
		}

		private bool HasValidChecksum(int pageSize)
		{
			uint expected = BitConverter.ToUInt32(_page, 22);

			// The checksum is computed with its own field zeroed.
			uint crc = 0;
			for (int i = 0; i < pageSize; i++)
			{
				byte value = i >= 22 && i < 26 ? (byte)0 : _page[i];
				crc = (crc << 8) ^ CrcTable[(crc >> 24) ^ value];
			}

			return crc == expected;
		}

		private static readonly uint[] CrcTable = BuildCrcTable();

		private static uint[] BuildCrcTable()
		{
			uint[] table = new uint[256];
			for (uint i = 0; i < 256; i++)
			{
				uint crc = i << 24;
				for (int bit = 0; bit < 8; bit++)
					crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04C11DB7 : crc << 1;

				table[i] = crc;
			}

			return table;
		}
	}
}
