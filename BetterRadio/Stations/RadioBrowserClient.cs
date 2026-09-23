using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using BetterRadio.Utilities;
using Newtonsoft.Json;

namespace BetterRadio.Stations
{
	internal class RadioBrowserClient
	{
		private const string UserAgent = "M_BetterRadio";
		private const string DiscoveryHost = "all.api.radio-browser.info";
		private const string FallbackHost = "de1.api.radio-browser.info";
		private const int RequestTimeoutMs = 8000;

		private static readonly object _hostLock = new object();
		private static string _cachedHost;

		public List<Station> Search(SearchQuery query, int offset, int limit)
		{
			string host = ResolveHost();
			string url = $"https://{host}/json/stations/search?{BuildQueryString(query)}&offset={offset}&limit={limit}&order=clickcount&reverse=true&hidebroken=true";

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = UserAgent;
			request.Timeout = RequestTimeoutMs;
			request.Accept = "application/json";

			using (WebResponse response = request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
			{
				string json = reader.ReadToEnd();
				return JsonConvert.DeserializeObject<List<Station>>(json) ?? new List<Station>();
			}
		}

		// The API is served from a rotating pool of community-run mirrors, discovered via a round-robin DNS entry.
		// The chosen host is cached for the process lifetime so only the first search pays the discovery cost.
		private static string ResolveHost()
		{
			lock (_hostLock)
			{
				if (_cachedHost != null)
					return _cachedHost;

				try
				{
					IPHostEntry entry = Dns.GetHostEntry(DiscoveryHost);
					List<string> hosts = new List<string>();

					foreach (IPAddress address in entry.AddressList)
					{
						try
						{
							string hostName = Dns.GetHostEntry(address).HostName;
							if (!hosts.Contains(hostName))
								hosts.Add(hostName);
						}
						catch (Exception ex)
						{
							// One mirror failing reverse DNS shouldn't sink discovery entirely.
							Logging.LogDebug($"Reverse DNS failed for {address}: {ex.Message}");
						}
					}

					if (hosts.Count > 0)
					{
						_cachedHost = hosts[new Random().Next(hosts.Count)];
						Logging.LogInfo($"Radio Browser server: {_cachedHost}.");
						return _cachedHost;
					}
				}
				catch (Exception ex)
				{
					Logging.LogWarning($"Radio Browser server discovery failed, using fallback: {ex.Message}");
				}

				_cachedHost = FallbackHost;
				return _cachedHost;
			}
		}

		// Only positive (non-negated) conditions are sent upstream as the server has no way to
		// express "not equal" for these fields. StationBrowser re-applies every condition, negation included,
		// against the results afterward, so correctness never depends on getting this mapping exactly right.
		private static string BuildQueryString(SearchQuery query)
		{
			StringBuilder builder = new StringBuilder();
			List<string> tags = new List<string>();

			void Append(string key, string value)
			{
				if (builder.Length > 0) builder.Append('&');
				builder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
			}

			foreach (SearchCondition condition in query.Conditions)
			{
				if (condition.Negate) continue;

				switch (condition.Field)
				{
					// Only the first positive Name condition is forwarded, since the API's name filter is one
					// substring match, not an AND of several words; the client-side filter enforces the rest.
					case SearchField.Name when !builder.ToString().Contains("name="):
						Append(condition.Exact ? "nameExact" : "name", condition.Value);
						break;

					case SearchField.Country when !builder.ToString().Contains("country="):
						Append(condition.Exact ? "countryExact" : "country", condition.Value);
						break;

					case SearchField.CountryCode when !builder.ToString().Contains("countrycode="):
						Append("countrycode", condition.Value);
						break;

					case SearchField.State when !builder.ToString().Contains("state="):
						Append(condition.Exact ? "stateExact" : "state", condition.Value);
						break;

					case SearchField.Language when !builder.ToString().Contains("language="):
						Append(condition.Exact ? "languageExact" : "language", condition.Value);
						break;

					case SearchField.Tag:
						tags.Add(condition.Value);
						break;

					case SearchField.Codec when !builder.ToString().Contains("codec="):
						Append("codec", condition.Value);
						break;

					case SearchField.BitrateMin when !builder.ToString().Contains("bitrateMin="):
						Append("bitrateMin", condition.Value);
						break;

					case SearchField.BitrateMax when !builder.ToString().Contains("bitrateMax="):
						Append("bitrateMax", condition.Value);
						break;
				}
			}

			if (tags.Count > 0)
				Append("tagList", string.Join(",", tags));

			return builder.ToString();
		}

	}
}
