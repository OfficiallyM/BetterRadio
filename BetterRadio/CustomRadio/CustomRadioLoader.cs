using System;
using System.Threading;
using BetterRadio.Utilities;
using UnityEngine;

namespace BetterRadio.CustomRadio
{
	public class CustomRadioLoader : MonoBehaviour
	{
		private static readonly IAudioDecoder[] _decoders =
		{
			new Mp3Decoder(),
			new OggDecoder(),
			new WavDecoder()
		};

		public static CustomRadioLoader Instance { get; private set; }

		private readonly CustomRadioLibrary _library = new CustomRadioLibrary();

		private volatile bool _isLoading;
		private volatile bool _hasResult;
		private volatile string _errorMessage;
		private DecodedAudio _pendingAudio;
		private string _pendingPath;

		public static void Initialize(GameObject helperObj)
		{
			Instance = helperObj.GetComponent<CustomRadioLoader>() ?? helperObj.AddComponent<CustomRadioLoader>();
			Instance.Rescan();
		}

		public void Rescan()
			=> _library.Rescan(settingsscript.s.S.SCustomRadioPath);

		public void LoadRandomSong()
		{
			if (_isLoading)
				return;

			string path = _library.PickNextTrack();

			Logging.LogDebug($"Picked song: {path}");

			if (path == null)
			{
				Logging.LogWarning("No custom radio tracks found in " + _library.GetPath());
				return;
			}

			_isLoading = true;
			_hasResult = false;
			_errorMessage = null;
			_pendingPath = path;

			ThreadPool.QueueUserWorkItem(DecodeOnBackgroundThread, path);
		}

		private void Update()
		{
			if (!_hasResult)
			{
				if (_errorMessage != null)
				{
					Logging.LogError("Custom radio failed to load " + _pendingPath + ": " + _errorMessage);
					_errorMessage = null;

					// Don't get stuck on one bad file - move straight on to the next one.
					_isLoading = false;
					LoadRandomSong();
				}

				return;
			}

			_hasResult = false;
			ApplyDecodedAudio(_pendingAudio);
			_isLoading = false;
		}

		private void DecodeOnBackgroundThread(object state)
		{
			string path = (string)state;

			try
			{
				IAudioDecoder decoder = FindDecoder(path);
				if (decoder == null)
					throw new NotSupportedException("No decoder registered for: " + path);

				_pendingAudio = decoder.Decode(path);
				_hasResult = true;
			}
			catch (Exception ex)
			{
				_errorMessage = ex.Message;
			}
		}

		private void ApplyDecodedAudio(DecodedAudio audio)
		{
			var channel = mainscript.M.radioChsAM[2];

			if (channel.currentClip != null)
				Destroy(channel.currentClip);

			int channelCount = Mathf.Max(1, audio.Channels);
			var clip = AudioClip.Create(
				"CustomRadioTrack",
				audio.Samples.Length / channelCount,
				channelCount,
				audio.SampleRate,
				false);

			clip.SetData(audio.Samples, 0);

			channel.currentClip = clip;
			channel.cTime = 0f;
			channel.startTime = Time.time;
			channel.cMaxTime = clip.length;

			Logging.LogDebug("Custom radio now playing: " + _pendingPath);
		}

		private static IAudioDecoder FindDecoder(string path)
		{
			foreach (var decoder in _decoders)
			{
				if (decoder.CanDecode(path))
					return decoder;
			}
			return null;
		}
	}
}