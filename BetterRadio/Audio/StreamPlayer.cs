using System;
using System.Threading;
using UnityEngine;

namespace BetterRadio.Audio
{
	internal class StreamPlayer
	{
		private const string ClipName = "BetterRadio stream";
		private const float StartBufferSeconds = 1.5f;

		private readonly AudioSource _source;
		private StreamConnection _connection;
		private AudioClip _clip;
		private int _underruns;

		internal StreamPlayer(AudioSource source)
		{
			_source = source;
		}

		internal bool IsPlaying { get; private set; }
		internal int Underruns => Interlocked.CompareExchange(ref _underruns, 0, 0);
		internal StreamStatus Status => _connection?.Status ?? StreamStatus.Idle;
		internal int SampleRate => _connection?.SampleRate ?? 0;
		internal int Channels => _connection?.Channels ?? 0;

		internal float BufferedSeconds
		{
			get
			{
				StreamConnection connection = _connection;
				if (connection == null) return 0f;

				int sampleRate = connection.SampleRate;
				if (sampleRate == 0) return 0f;

				return connection.Buffer.Available / (float)(sampleRate * connection.Channels);
			}
		}

		internal void Start(string url)
		{
			Stop();
			Interlocked.Exchange(ref _underruns, 0);

			_connection = new StreamConnection(url);
			_connection.Start();
		}

		internal void Stop()
		{
			if (_connection != null)
			{
				_connection.Cancel();
				_connection = null;
			}

			_source.Stop();
			_source.clip = null;

			if (_clip != null)
			{
				UnityEngine.Object.Destroy(_clip);
				_clip = null;
			}

			IsPlaying = false;
		}

		// Must be called every frame from the main thread, as the clip and source can only be touched there.
		internal void Update()
		{
			if (_connection == null || IsPlaying) return;

			// Sample rate is read first because the connection publishes it after the buffer and channels.
			int sampleRate = _connection.SampleRate;
			if (sampleRate == 0) return;

			RingBuffer buffer = _connection.Buffer;
			int channels = _connection.Channels;
			if (buffer.Available < sampleRate * channels * StartBufferSeconds) return;

			// The callback captures the buffer instead of reading the connection field so a late audio thread call after teardown stays harmless.
			_clip = AudioClip.Create(ClipName, sampleRate * 2, channels, sampleRate, true, data => OnPcmRead(buffer, data), OnPcmSetPosition);
			_source.clip = _clip;

			// Looping keeps playback going past the clip's nominal length, which means nothing for a live stream.
			_source.loop = true;
			_source.Play();
			IsPlaying = true;
		}

		// Runs on the audio thread, so nothing here may touch the Unity API.
		private void OnPcmRead(RingBuffer buffer, float[] data)
		{
			int read = buffer.Read(data, 0, data.Length);
			if (read >= data.Length) return;

			Array.Clear(data, read, data.Length - read);
			Interlocked.Increment(ref _underruns);
		}

		// A live stream has no position to seek to, but Unity expects a callback when the looping clip wraps.
		private void OnPcmSetPosition(int position)
		{
		}
	}
}
