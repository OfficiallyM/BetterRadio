using Newtonsoft.Json;

namespace BetterRadio.Stations
{
	internal class Station
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("url_resolved")]
		public string UrlResolved { get; set; }

		[JsonProperty("codec")]
		public string Codec { get; set; }

		[JsonProperty("bitrate")]
		public int Bitrate { get; set; }

		[JsonProperty("country")]
		public string Country { get; set; }

		[JsonProperty("countrycode")]
		public string CountryCode { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		[JsonProperty("language")]
		public string Language { get; set; }

		[JsonProperty("tags")]
		public string Tags { get; set; }

	}
}
