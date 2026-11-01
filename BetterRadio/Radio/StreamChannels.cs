using BetterRadio.Stations;
using BetterRadio.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace BetterRadio.Radio
{
	// Keeps the game's FM channel list in step with the saved stations: 99.2 FM is always there as the pick-a-station
	// channel, and every station with an assigned frequency gets a channel of its own that the dial can tune to.
	internal static class StreamChannels
	{
		internal const float MinFrequency = 87.5f;
		internal const float MaxFrequency = 108f;

		// Channel frequencies are compared loosely, since they pass through float arithmetic in the game.
		private const float Tolerance = 0.05f;

		// Our channels are tracked by frequency, which stays correct whether the game rebuilds its list or not.
		private static readonly Dictionary<SavedStation, float> _assigned = new Dictionary<SavedStation, float>();
		private static KeyValuePair<SavedStation, float>[] _lookup = new KeyValuePair<SavedStation, float>[0];
		private static StationRepository _repository;

		internal static void Initialise(StationRepository repository)
		{
			if (_repository == null)
			{
				_repository = repository;
				_repository.Changed += Sync;
			}

			if (!mainscript.M.radioChs.Any(c => Matches(c.FR, BetterRadio.StreamFR)))
				mainscript.M.radioChs.Add(CreateChannel("stream", BetterRadio.StreamFR));

			Sync();
		}

		internal static bool IsStreamFrequency(float frequency)
		{
			return Matches(frequency, BetterRadio.StreamFR) || GetStation(frequency) != null;
		}

		// The station assigned to this frequency, or null for 99.2 FM and anything else.
		// Runs several times a frame from the radio patches, so it walks a cached array instead of querying the repository.
		internal static SavedStation GetStation(float frequency)
		{
			foreach (KeyValuePair<SavedStation, float> pair in _lookup)
			{
				if (Matches(pair.Value, frequency))
					return pair.Key;
			}

			return null;
		}

		// Parses and checks a frequency typed into the UI. Returns null and an error message when it cannot be used.
		internal static float? ParseFrequency(string text, SavedStation station, out string error)
		{
			string normalised = (text ?? string.Empty).Trim().Replace(',', '.');
			if (normalised.EndsWith("fm", StringComparison.OrdinalIgnoreCase))
				normalised = normalised.Substring(0, normalised.Length - 2).Trim();

			if (!float.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
			{
				error = "Enter a frequency such as 101.5";
				return null;
			}

			float frequency = (float)Math.Round(parsed, 1);

			if (frequency < MinFrequency || frequency > MaxFrequency)
				error = $"Must be between {Format(MinFrequency)} and {Format(MaxFrequency)} FM";
			else if (Matches(frequency, BetterRadio.StreamFR))
				error = $"{Format(BetterRadio.StreamFR)} FM is the station picker channel";
			else if (_repository?.Stations.FirstOrDefault(s => s != station && s.Frequency.HasValue && Matches(s.Frequency.Value, frequency)) is SavedStation other)
				error = $"Already used by {other.Name}";
			else if (IsGameChannel(frequency))
				error = $"{Format(frequency)} FM is one of the game's own stations";
			else
				error = null;

			return error == null ? frequency : (float?)null;
		}

		internal static string Format(float frequency) => frequency.ToString("0.0", CultureInfo.InvariantCulture);

		private static void Sync()
		{
			if (_repository == null || mainscript.M == null) return;

			List<SavedStation> stations = _repository.Stations.ToList();

			// Drop channels for stations that were removed, unassigned or moved to another frequency.
			foreach (KeyValuePair<SavedStation, float> pair in _assigned.ToList())
			{
				SavedStation station = pair.Key;
				bool stillValid = stations.Contains(station) && station.Frequency.HasValue && Matches(station.Frequency.Value, pair.Value);
				if (stillValid && HasChannel(pair.Value)) continue;

				mainscript.M.radioChs.RemoveAll(c => Matches(c.FR, pair.Value));
				_assigned.Remove(station);
			}

			foreach (SavedStation station in stations)
			{
				if (!station.Frequency.HasValue || _assigned.ContainsKey(station)) continue;

				float frequency = station.Frequency.Value;

				// A frequency saved before a game update claimed it is left alone rather than fighting the game's station.
				if (IsGameChannel(frequency))
				{
					Logging.LogWarning($"{Format(frequency)} FM is taken by the game, so {station.Name} was not given a channel.");
					continue;
				}

				mainscript.M.radioChs.Add(CreateChannel(station.Name, frequency));
				_assigned.Add(station, frequency);
				Logging.LogDebug($"Assigned {station.Name} to {Format(frequency)} FM.");
			}

			_lookup = _assigned.ToArray();
		}

		private static bool HasChannel(float frequency) => mainscript.M.radioChs.Any(c => Matches(c.FR, frequency));

		private static bool IsGameChannel(float frequency)
		{
			if (mainscript.M == null || Matches(frequency, BetterRadio.StreamFR)) return false;
			if (_assigned.Values.Any(f => Matches(f, frequency))) return false;

			return HasChannel(frequency);
		}

		private static bool Matches(float a, float b) => Mathf.Abs(a - b) < Tolerance;

		private static mainscript.radioCH CreateChannel(string name, float frequency)
		{
			// The game expects these lists to exist even though a stream channel never plays any of the game's own clips.
			return new mainscript.radioCH()
			{
				name = name,
				FR = frequency,
				music = new List<AudioClip>(),
				talk = new List<AudioClip>(),
				musicHoliday = new List<mainscript.holiday>(),
				talkHoliday = new List<mainscript.holiday>(),
				avaiableMusic = new List<int>(),
				avaiableTalk = new List<int>(),
			};
		}
	}
}
