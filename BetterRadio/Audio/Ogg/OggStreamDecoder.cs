using BetterRadio.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterRadio.Audio.Ogg
{
	// Demuxes an Ogg stream and hands each logical stream to the right codec (Vorbis, Opus or FLAC).
	// Icecast starts a new chained logical stream at most track changes, so when one ends this moves on to the next.
	internal class OggStreamDecoder : BufferedStreamDecoder
	{
		private readonly OggPageReader _pages;
		private readonly Queue<byte[]> _packets = new Queue<byte[]>();
		private readonly EmitHandler _emit;
		private byte[] _partialPacket;
		private OggPage _pendingPage;
		private int _serial;
		private bool _logicalStreamEnded;
		private IOggCodec _codec;

		internal OggStreamDecoder(PeekableStream stream)
		{
			_pages = new OggPageReader(stream);
			_emit = (samples, offset, frames, sampleRate, channels, left, right) => Emit(samples, offset, frames, sampleRate, channels, left, right);
		}

		protected override bool DecodeNextBlock()
		{
			if (_codec == null)
				return StartNextLogicalStream();

			if (!_codec.DecodeNext())
			{
				_codec.Dispose();
				_codec = null;
			}

			return true;
		}

		// Skips forward to the next logical stream in a codec this can decode. Returns false if the stream ends first.
		private bool StartNextLogicalStream()
		{
			_packets.Clear();
			_partialPacket = null;
			_logicalStreamEnded = false;

			while (true)
			{
				OggPage page = TakePage();
				if (page == null) return false;
				if (!page.IsBeginningOfStream || page.Packets.Count == 0) continue;

				byte[] first = page.Packets[0];
				Func<IOggCodec> create = ChooseCodec(first, page.Serial);
				if (create == null)
				{
					Logging.LogDebug("Skipping an Ogg logical stream in an unsupported codec.");
					continue;
				}

				_serial = page.Serial;
				AddPage(page);
				_codec = create();
				return true;
			}
		}

		private Func<IOggCodec> ChooseCodec(byte[] firstPacket, int serial)
		{
			if (StartsWith(firstPacket, 0x01, "vorbis"))
				return () => new OggVorbisCodec(NextPacket, serial, _emit);

			if (StartsWith(firstPacket, null, "OpusHead"))
				return () => new OggOpusCodec(NextPacket, _emit);

			if (StartsWith(firstPacket, 0x7F, "FLAC"))
				return () => new OggFlacCodec(NextPacket, _emit);

			return null;
		}

		private static bool StartsWith(byte[] packet, byte? prefix, string magic)
		{
			int offset = prefix.HasValue ? 1 : 0;
			if (packet.Length < offset + magic.Length) return false;
			if (prefix.HasValue && packet[0] != prefix.Value) return false;

			return Encoding.ASCII.GetString(packet, offset, magic.Length) == magic;
		}

		// Next packet of the current logical stream, or null once it has ended.
		private byte[] NextPacket()
		{
			while (_packets.Count == 0)
			{
				if (_logicalStreamEnded) return null;

				OggPage page = TakePage();
				if (page == null)
				{
					_logicalStreamEnded = true;
					return null;
				}

				if (page.Serial != _serial)
				{
					// A new chain link starting means the current one is over, even if its end page never arrived.
					if (page.IsBeginningOfStream)
					{
						_pendingPage = page;
						_logicalStreamEnded = true;
					}

					continue;
				}

				AddPage(page);
			}

			return _packets.Dequeue();
		}

		private OggPage TakePage()
		{
			OggPage page = _pendingPage ?? _pages.ReadPage();
			_pendingPage = null;
			return page;
		}

		private void AddPage(OggPage page)
		{
			int last = page.Packets.Count - 1;
			for (int i = 0; i <= last; i++)
			{
				byte[] data = page.Packets[i];

				if (i == 0)
				{
					if (page.IsContinuation && _partialPacket != null)
					{
						data = Concat(_partialPacket, data);
					}
					else if (page.IsContinuation)
					{
						// The start of this packet was on a page that was lost or skipped, so the tail is useless.
						if (i == last && page.LastPacketIsPartial) _partialPacket = null;
						continue;
					}

					_partialPacket = null;
				}

				if (i == last && page.LastPacketIsPartial)
					_partialPacket = data;
				else
					_packets.Enqueue(data);
			}

			if (page.IsEndOfStream)
				_logicalStreamEnded = true;
		}

		private static byte[] Concat(byte[] first, byte[] second)
		{
			byte[] result = new byte[first.Length + second.Length];
			Array.Copy(first, result, first.Length);
			Array.Copy(second, 0, result, first.Length, second.Length);
			return result;
		}

		public override void Dispose()
		{
			_codec?.Dispose();
			_codec = null;
		}
	}
}
