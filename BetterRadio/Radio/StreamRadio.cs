using BetterRadio.Audio;
using BetterRadio.Stations;
using UnityEngine;

namespace BetterRadio.Radio
{
	internal class StreamRadio : MonoBehaviour
	{
		private radioscript _radio;
		private StreamPlayer _player;
		private string _url;
		private string _playingUrl;
		private bool _isRunning = false;
		public bool IsRunning => _isRunning;

		private void Start()
		{
			_radio = GetComponent<radioscript>();
			_player = new StreamPlayer(_radio.SC);
			StreamRadioManager.Add(_radio.tosave.idInSave, this);
		}

		private void OnDestroy()
		{
			StreamRadioManager.Remove(_radio.tosave.idInSave);
		}

		// The URL for whatever this radio is tuned to: an assigned station's own frequency, or 99.2 FM's picked station.
		public string CurrentUrl => StreamChannels.GetStation(_radio.FR)?.Url ?? _url;

		// Display name for the UI, or null if nothing is set to play.
		public string CurrentStationName
		{
			get
			{
				SavedStation assigned = StreamChannels.GetStation(_radio.FR);
				if (assigned != null) return $"{assigned.Name} ({StreamChannels.Format(assigned.Frequency.Value)} FM)";
				if (string.IsNullOrEmpty(_url)) return null;

				return BetterRadio.Stations.GetByUrl(_url)?.Name ?? _url;
			}
		}

		public bool IsOnAssignedFrequency => StreamChannels.GetStation(_radio.FR) != null;

		public void StartPlayer()
		{
			string url = CurrentUrl;
			if (string.IsNullOrEmpty(url))
			{
				_player.Stop();
				return;
			}
			_player.Start(url);
			_playingUrl = url;
			_isRunning = true;
		}

		public void StopPlayer()
		{
			_player.Stop();
			_playingUrl = null;
			_isRunning = false;
		}

		// Sets the station 99.2 FM plays on this radio.
		public void SetUrl(string url)
		{
			_url = url;
			StopPlayer();
		}

		public string GetUrl()
			=> _url;

		public void Tick(bool isStreamMode)
		{
			// Turning the dial from one stream channel to another, or reassigning a frequency, switches stream.
			if (isStreamMode && _isRunning && CurrentUrl != _playingUrl)
				StopPlayer();

			if (isStreamMode && !_isRunning && _radio.v > 0)
				StartPlayer();

			if (!isStreamMode && _isRunning)
				StopPlayer();

			_player.Update();

			if (!isStreamMode) return;

			_radio.SC.volume = Mathf.Lerp(0f, _radio.electronics.output, _radio.v);

			if (_radio.v <= 0)
				StopPlayer();
		}

		public StreamPlayer GetPlayer() => _player;
	}
}
