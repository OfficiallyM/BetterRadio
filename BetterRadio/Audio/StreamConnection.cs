using BetterRadio.Utilities;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace BetterRadio.Audio
{
	// Single use: a new instance is created for every connection attempt so a stale thread can never see a reset cancel flag.
	internal class StreamConnection
	{
		private const string UserAgent = "BetterRadio";
		private const int ConnectTimeoutMs = 10000;
		private const int ReadTimeoutMs = 15000;
		private const int DecodeChunkSamples = 4096;
		private const float BufferSeconds = 5f;
		private const int WriteRetryDelayMs = 10;

		private readonly string _url;
		private readonly Thread _thread;
		private volatile HttpWebRequest _request;
		private volatile bool _cancelled;
		private volatile StreamStatus _status = StreamStatus.Idle;
		private volatile int _channels;
		private volatile int _sampleRate;
		private RingBuffer _buffer;

		internal StreamConnection(string url)
		{
			_url = url;
			_thread = new Thread(Run)
			{
				IsBackground = true,
				Name = "BetterRadio stream",
			};
		}

		internal StreamStatus Status => _status;
		internal int Channels => _channels;
		internal int SampleRate => _sampleRate;
		internal RingBuffer Buffer => _buffer;

		internal void Start()
		{
			_status = StreamStatus.Connecting;
			_thread.Start();
		}

		internal void Cancel()
		{
			_cancelled = true;

			// The worker is usually blocked on a network read, so aborting the request is what actually wakes it.
			try
			{
				_request?.Abort();
			}
			catch (Exception ex)
			{
				Logging.LogDebug($"Abort threw: {ex.Message}");
			}
		}

		private void Run()
		{
			try
			{
				Logging.LogDebug($"Connecting to {_url}.");

				// The Icy-MetaData header is deliberately not sent so the stream stays plain audio for now.
				_request = (HttpWebRequest)WebRequest.Create(_url);
				_request.UserAgent = UserAgent;
				_request.Timeout = ConnectTimeoutMs;
				_request.ReadWriteTimeout = ReadTimeoutMs;

				using (WebResponse response = _request.GetResponse())
				{
					Logging.LogDebug($"Content type: {response.ContentType}.");
					RunDirect(response);
				}

				_status = StreamStatus.Ended;
			}
			catch (Exception ex)
			{
				// Aborting the request to cancel surfaces as an exception on this thread, which is expected.
				if (_cancelled)
				{
					_status = StreamStatus.Ended;
				}
				else
				{
					Logging.LogError($"Stream failed: {ex}");
					_status = StreamStatus.Failed;
				}
			}
			finally
			{
				Logging.LogDebug("Stream thread exited.");
			}
		}

		private void RunDirect(WebResponse response)
		{
			using Stream stream = new ForwardOnlyStream(response.GetResponseStream());
			using IStreamDecoder decoder = StreamDecoderFactory.Create(response.ContentType, stream);
			Decode(decoder);
		}

		private void Decode(IStreamDecoder decoder)
		{
			float[] chunk = new float[DecodeChunkSamples];

			while (!_cancelled)
			{
				int read = decoder.ReadSamples(chunk, 0, chunk.Length);
				if (read <= 0) return;

				// Format is only trusted once the first samples have decoded.
				if (_buffer == null)
					Initialise(decoder);

				WriteAll(chunk, read);
			}
		}

		private void Initialise(IStreamDecoder decoder)
		{
			int channels = decoder.Channels;
			int sampleRate = decoder.SampleRate;

			_buffer = new RingBuffer((int)(sampleRate * channels * BufferSeconds), channels);
			_channels = channels;

			// Written last because the volatile write publishes everything above it to the main thread.
			_sampleRate = sampleRate;
			_status = StreamStatus.Streaming;

			Logging.LogDebug($"Stream format: {sampleRate} Hz, {channels} channel(s).");
		}

		private void WriteAll(float[] data, int count)
		{
			int offset = 0;
			while (offset < count && !_cancelled)
			{
				int written = _buffer.Write(data, offset, count - offset);
				offset += written;

				// A full buffer is the normal steady state, since playback drains it at real time.
				if (written == 0)
					Thread.Sleep(WriteRetryDelayMs);
			}
		}
	}
}