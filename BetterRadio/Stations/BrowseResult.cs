namespace BetterRadio.Stations
{
	internal class BrowseResult
	{
		public BrowseResult(string name, string url, string codec, int bitrate, string countryCode)
		{
			Name = name;
			Url = url;
			Codec = codec;
			Bitrate = bitrate;
			CountryCode = countryCode;
		}

		public string Name { get; }
		public string Url { get; }
		public string Codec { get; }
		public int Bitrate { get; }
		public string CountryCode { get; }
	}
}
