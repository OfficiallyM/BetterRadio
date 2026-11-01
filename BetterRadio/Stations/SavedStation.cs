namespace BetterRadio.Stations
{
	internal class SavedStation
	{
		public string Name { get; set; }
		public string Url { get; set; }

		// The FM frequency this station is tuned in at, or null if it is only reachable through the 99.2 FM picker.
		public float? Frequency { get; set; }
	}
}
