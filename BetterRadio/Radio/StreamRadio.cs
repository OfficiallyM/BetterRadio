using BetterRadio.Audio;
using UnityEngine;

namespace BetterRadio.Radio
{
	internal class StreamRadio : MonoBehaviour
	{
		private radioscript _radio;
		private StreamPlayer _player;
		private string _url;
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

		public void StartPlayer()
		{
			if (string.IsNullOrEmpty(_url))
			{
				_player.Stop();
				return;
			}
			_player.Start(_url);
			_isRunning = true;
		}

		public void StopPlayer()
		{
			_player.Stop();
			_isRunning = false;
		}

		public void SetUrl(string url)
		{
			_url = url;
			StopPlayer();
		}

		public string GetUrl()
			=> _url;

		public void Tick(bool isStreamMode)
		{
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
