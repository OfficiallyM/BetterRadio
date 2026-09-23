using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterRadio.Utilities;
using Newtonsoft.Json;

namespace BetterRadio.Stations
{
	internal class StationRepository
	{
		private readonly string _path;
		private List<SavedStation> _stations;

		public StationRepository(string path)
		{
			_path = path;
			_stations = Load();
		}

		// A copy is handed out so callers can iterate while Add() and Remove() mutate the backing list.
		public IReadOnlyList<SavedStation> Stations => _stations.ToList();

		public void Add(SavedStation station)
		{
			_stations.Add(station);
			Save();
		}

		public void Remove(SavedStation station)
		{
			_stations.Remove(station);
			Save();
		}

		public SavedStation GetByUrl(string url)
		{
			return _stations.FirstOrDefault(s => s.Url == url);
		}

		private List<SavedStation> Load()
		{
			if (!File.Exists(_path))
				return new List<SavedStation>();

			try
			{
				string json = File.ReadAllText(_path);
				return JsonConvert.DeserializeObject<List<SavedStation>>(json) ?? new List<SavedStation>();
			}
			catch (JsonException ex)
			{
				// A corrupt file loses the saved list rather than crashing the mod on load.
				Logging.LogError($"Failed to read {_path}, starting with an empty station list: {ex.Message}");
				return new List<SavedStation>();
			}
		}

		private void Save()
		{
			string json = JsonConvert.SerializeObject(_stations, Formatting.Indented);
			File.WriteAllText(_path, json);
		}
	}
}
