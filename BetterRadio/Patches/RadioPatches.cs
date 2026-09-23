using BetterRadio.Radio;
using HarmonyLib;

namespace BetterRadio.Patches
{
	[HarmonyPatch(typeof(radioscript), nameof(radioscript.SetRadio))]
	internal static class Patch_Radio_SetRadio
	{
		private static bool Prefix(radioscript __instance)
		{
			var streamMode = __instance.IsStreamMode();
			if (streamMode)
			{
				__instance.SS.volume = 0;
				__instance.SS.Stop();
				__instance.SC2.volume = 0;
				__instance.SC2.Stop();
			}
			StreamRadioManager.Get(__instance.tosave.idInSave)?.Tick(streamMode);
			return !streamMode;
		}
	}

	[HarmonyPatch(typeof(radioscript), "Update")]
	internal static class Patch_Radio_Update
	{
		private static bool Prefix(radioscript __instance)
		{
			bool skip = false;

			if (__instance.IsStreamMode())
			{
				if (__instance.BChangeAMFM != null && __instance.BChangeAMFM.turned)
				{
					skip = true;
					__instance.BChangeAMFM.turned = false;
					mainscript.PlayClipAtPoint(__instance.Cturned, __instance.SS.transform.position, __instance.turnVolume, randomPitch: true, mainscript.AudioPriorities[4]);

					BetterRadio.UI.ShowUI(StreamRadioManager.Get(__instance.tosave.idInSave));
				}
			}

			return !skip;
		}
	}

	[HarmonyPatch(typeof(usablescript), nameof(usablescript.GetTurnText))]
	internal static class Patch_Usable_GetTurnText
	{
		private static bool Prefix(usablescript __instance, ref string __result)
		{
			bool skip = false;

			var radio = __instance.transform?.parent?.GetComponentInChildren<radioscript>();
			if (radio?.IsStreamMode() ?? false)
			{
				__result = "stream configuration";
				skip = true;
			}

			return !skip;
		}
	}
}
