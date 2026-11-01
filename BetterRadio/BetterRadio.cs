using BetterRadio.CustomRadio;
using BetterRadio.Radio;
using BetterRadio.Stations;
using BetterRadio.UI;
using System.Collections.Generic;
using System.IO;
using TLDLoader;
using UnityEngine;

namespace BetterRadio
{
	public class BetterRadio : Mod
	{
		// Mod meta stuff.
		private string _version = "1.1.0";
		public override string ID => "M_BetterRadio";
		public override string Name => "BetterRadio";
		public override string Author => "M-";
		public override string Version => _version;
		public override bool UseLogger => true;
		public override bool LoadInDB => true;
		public override bool UseHarmony => true;

		internal static BetterRadio Instance;
		internal static bool Debug = false;

		internal static float StreamFR = 99.2f;
		internal static UIManager UI;
		internal static StationRepository Stations;

		private GameObject _helperObj;

		public BetterRadio()
		{
			Instance = this;
#if DEBUG
			_version += "-DEV";
			Debug = true;
#endif
		}

		public override void DbLoad()
		{
			foreach (var item in itemdatabase.d.items)
			{
				var radio = item.GetComponentInChildren<radioscript>();
				if (radio != null && radio.GetComponent<StreamRadio>() == null)
					radio.gameObject.AddComponent<StreamRadio>();
			}

			Stations ??= new StationRepository(Path.Combine(ModLoader.GetModConfigFolder(this), "Stations.json"));
			StreamChannels.Initialise(Stations);

			_helperObj ??= new GameObject("BetterRadio");
			GameObject.DontDestroyOnLoad(_helperObj);
			UI = _helperObj.GetComponent<UIManager>() ?? _helperObj.AddComponent<UIManager>();
			CustomRadioLoader.Initialize(_helperObj);
		}

		public override void OnLoad()
		{
			foreach (KeyValuePair<int, tosaveitemscript> keyValuePair in savedatascript.d.toSaveStuff)
			{
				if (keyValuePair.Value == null) continue;
				var radio = keyValuePair.Value.GetComponentInChildren<radioscript>();
				if (radio != null && radio.GetComponent<StreamRadio>() == null)
					radio.gameObject.AddComponent<StreamRadio>();
			}
		}

		public override void OnGUI()
		{
			Styling.Bootstrap();
		}
	}
}
