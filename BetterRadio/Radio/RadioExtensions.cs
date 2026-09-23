using UnityEngine;

namespace BetterRadio.Radio
{
	internal static class RadioExtensions
	{
		public static bool IsStreamMode(this radioscript radio)
		{
			return Mathf.Approximately(radio?.FR ?? 0, BetterRadio.StreamFR);
		}
	}
}
