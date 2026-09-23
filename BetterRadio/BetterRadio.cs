using BetterRadio.Radio;
using BetterRadio.UI;
using System.Collections.Generic;
using TLDLoader;
using UnityEngine;

namespace BetterRadio
{
	public class BetterRadio : Mod
	{
		// Mod meta stuff.
		private string _version = "1.0.0";
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

			mainscript.M.radioChs.Add(new mainscript.radioCH()
			{
				name = "stream",
				FR = StreamFR,
				music = new List<AudioClip>(),
				talk = new List<AudioClip>(),
				musicHoliday = new List<mainscript.holiday>(),
				talkHoliday = new List<mainscript.holiday>(),
				avaiableMusic = new List<int>(),
				avaiableTalk = new List<int>(),
			});

			_helperObj ??= new GameObject("StreamRadio");
			GameObject.DontDestroyOnLoad(_helperObj);
			UI = _helperObj.GetComponent<UIManager>() ?? _helperObj.AddComponent<UIManager>();
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
