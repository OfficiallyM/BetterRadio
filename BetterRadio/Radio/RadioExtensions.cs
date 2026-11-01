namespace BetterRadio.Radio
{
	internal static class RadioExtensions
	{
		public static bool IsStreamMode(this radioscript radio)
		{
			return radio != null && StreamChannels.IsStreamFrequency(radio.FR);
		}
	}
}
