using System;
using System.Collections.Generic;
using System.IO;
using BetterRadio.Utilities;
using UnityEngine;

namespace BetterRadio.CustomRadio
{
    public class CustomRadioLibrary
    {
        private static readonly string[] _supportedExtensions = { ".mp3", ".wav", ".ogg" };

		private string _path;
		private readonly List<string> _tracks = new List<string>();
        private readonly List<int> _notYetPlayed = new List<int>();
        private int _currentSong = -1;

        public bool HasTracks => _tracks.Count > 0;

        public void Rescan(string folderPath)
        {
			_tracks.Clear();
            _notYetPlayed.Clear();
            _currentSong = -1;
			_path = folderPath;

            if (!Directory.Exists(folderPath))
            {
                Logging.LogWarning("Custom radio folder does not exist: " + folderPath);
                return;
            }

            foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                if (IsSupported(file))
					_tracks.Add(file);
            }

            Logging.LogDebug("Custom radio scan found " + _tracks.Count + " track(s) in " + folderPath);
        }

		public string GetPath()
			=> _path;

        public string PickNextTrack()
        {
            if (_tracks.Count == 0)
                return null;

            if (_notYetPlayed.Count == 0)
            {
                for (int i = 0; i < _tracks.Count; i++)
                    _notYetPlayed.Add(i);
            }

            if (_tracks.Count > 1 && _notYetPlayed.Contains(_currentSong))
                _notYetPlayed.Remove(_currentSong);

            int index = _notYetPlayed[UnityEngine.Random.Range(0, _notYetPlayed.Count)];
            _notYetPlayed.Remove(index);
            _currentSong = index;

            return _tracks[_currentSong];
        }

        private static bool IsSupported(string path)
        {
            foreach (var extension in _supportedExtensions)
            {
                if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
