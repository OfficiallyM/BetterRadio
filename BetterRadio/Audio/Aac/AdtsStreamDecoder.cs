using BetterRadio.Utilities;
using SharpJaad.AAC;
using System;

namespace BetterRadio.Audio.Aac
{
	// Decodes AAC, HE-AAC (AAC+) and HE-AACv2 carried in ADTS frames, which is how Shoutcast and Icecast serve AAC.
	// SharpJaad only understands raw AAC frames, so this strips the ADTS framing and builds the decoder config from it.
	internal class AdtsStreamDecoder : BufferedStreamDecoder
	{
		private const int HeaderSize = 7;
		private const int CrcSize = 2;

		// Give up on a stream that never produces a decodable frame rather than spinning on garbage forever.
		private const int MaxConsecutiveErrors = 50;

		private static readonly int[] SampleRates = { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350 };

		private readonly PeekableStream _stream;
		private readonly byte[] _header = new byte[HeaderSize];
		private readonly SampleBuffer _sampleBuffer = new SampleBuffer();
		private Decoder _decoder;
		private int _configKey = -1;
		private bool _synced;
		private int _consecutiveErrors;
		private float[] _floats = new float[4096];

		internal AdtsStreamDecoder(PeekableStream stream)
		{
			_stream = stream;
		}

		// True if the bytes at offset look like the start of an ADTS header: 12 sync bits then a layer field of zero.
		// The layer check is what separates ADTS from an MPEG audio (MP3) frame, which shares the sync word.
		internal static bool IsAdtsSync(byte first, byte second) => first == 0xFF && (second & 0xF6) == 0xF0;

		protected override bool DecodeNextBlock()
		{
			if (!ReadFrameHeader(out int objectType, out int sampleRateIndex, out int channelConfig, out int payloadLength))
				return false;

			byte[] payload = new byte[payloadLength];
			if (!_stream.TryReadFully(payload, 0, payloadLength))
				return false;

			try
			{
				EnsureDecoder(objectType, sampleRateIndex, channelConfig);
				_decoder.DecodeFrame(payload, _sampleBuffer);
				_consecutiveErrors = 0;
			}
			catch (Exception ex)
			{
				// One bad frame (a dropped packet, a glitch at a track change) should not kill the whole stream.
				if (++_consecutiveErrors >= MaxConsecutiveErrors)
					throw new InvalidOperationException($"AAC stream could not be decoded: {ex.Message}", ex);

				Logging.LogDebug($"Skipped undecodable AAC frame: {ex.Message}");

				// The decoder's state may be left inconsistent by a failed frame, so start afresh.
				_decoder = null;
				_configKey = -1;
				return true;
			}

			EmitPcm();
			return true;
		}

		private bool ReadFrameHeader(out int objectType, out int sampleRateIndex, out int channelConfig, out int payloadLength)
		{
			objectType = sampleRateIndex = channelConfig = payloadLength = 0;

			while (true)
			{
				if (!FindSync())
					return false;

				if (!_stream.TryPeek(HeaderSize - 1, out _))
					return false;

				_stream.Peek(_header, HeaderSize);

				bool protectionAbsent = (_header[1] & 0x01) != 0;
				objectType = ((_header[2] >> 6) & 0x03) + 1;
				sampleRateIndex = (_header[2] >> 2) & 0x0F;
				channelConfig = ((_header[2] & 0x01) << 2) | (_header[3] >> 6);
				int frameLength = ((_header[3] & 0x03) << 11) | (_header[4] << 3) | (_header[5] >> 5);
				int headerLength = protectionAbsent ? HeaderSize : HeaderSize + CrcSize;

				bool valid = sampleRateIndex < SampleRates.Length && channelConfig != 0 && frameLength > headerLength;

				// When joining a stream mid-frame, a stray 0xFFF in the audio data can look like a header, so the first
				// frame is only trusted if another header follows exactly where this one says it ends.
				if (valid && !_synced)
				{
					valid = _stream.TryPeek(frameLength, out byte next0) && _stream.TryPeek(frameLength + 1, out byte next1) && IsAdtsSync(next0, next1);
				}

				if (!valid)
				{
					_synced = false;
					_stream.Skip(1);
					continue;
				}

				_synced = true;
				_stream.Skip(headerLength);
				payloadLength = frameLength - headerLength;
				return true;
			}
		}

		// Advances the stream to the next ADTS sync word.
		private bool FindSync()
		{
			while (true)
			{
				if (!_stream.TryPeek(0, out byte first) || !_stream.TryPeek(1, out byte second))
					return false;

				if (IsAdtsSync(first, second))
					return true;

				_synced = false;
				_stream.Skip(1);
			}
		}

		private void EnsureDecoder(int objectType, int sampleRateIndex, int channelConfig)
		{
			int key = (objectType << 8) | (sampleRateIndex << 4) | channelConfig;
			if (_decoder != null && key == _configKey) return;

			// Builds the two byte AudioSpecificConfig: 5 bits object type, 4 bits sample rate index, 4 bits channel config.
			// HE-AAC is signalled implicitly inside the frames, which the decoder picks up on its own.
			byte[] audioSpecificConfig =
			{
				(byte)((objectType << 3) | (sampleRateIndex >> 1)),
				(byte)(((sampleRateIndex & 0x01) << 7) | (channelConfig << 3)),
			};

			_decoder = new Decoder(audioSpecificConfig);
			_configKey = key;
		}

		// SharpJaad hands back 16 bit big-endian PCM.
		private void EmitPcm()
		{
			byte[] data = _sampleBuffer.Data;
			int channels = _sampleBuffer.Channels;
			int sampleRate = _sampleBuffer.SampleRate;
			if (data == null || data.Length == 0 || channels <= 0) return;

			int sampleCount = data.Length / 2;
			if (_floats.Length < sampleCount)
				_floats = new float[sampleCount];

			bool bigEndian = _sampleBuffer.BigEndian;
			for (int i = 0; i < sampleCount; i++)
			{
				int high = bigEndian ? data[i * 2] : data[i * 2 + 1];
				int low = bigEndian ? data[i * 2 + 1] : data[i * 2];
				_floats[i] = (short)((high << 8) | low) / 32768f;
			}

			// AAC orders surround channels centre first (C, L, R, ...), unlike stereo's plain L, R.
			bool surround = channels > 2;
			Emit(_floats, 0, sampleCount / channels, sampleRate, channels, surround ? 1 : 0, surround ? 2 : 1);
		}
	}
}
