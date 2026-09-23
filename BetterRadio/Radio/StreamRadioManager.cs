using BetterRadio.Utilities;
using System.Collections.Generic;

namespace BetterRadio.Radio
{
	internal static class StreamRadioManager
	{
		private static Dictionary<int, StreamRadio> _instances = new Dictionary<int, StreamRadio>();

		public static void Add(int id, StreamRadio radio)
		{
			if (_instances.ContainsKey(id)) return;
			_instances.Add(id, radio);
			Logging.LogDebug($"Added StreamRadio {id}");
		}

		public static void Remove(int id)
		{
			_instances.Remove(id);
		}

		public static StreamRadio Get(int id)
		{
			return _instances.ContainsKey(id) ? _instances[id] : null;
		}
	}
}
