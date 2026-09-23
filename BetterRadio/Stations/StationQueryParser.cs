using System;
using System.Collections.Generic;
using System.Text;

namespace BetterRadio.Stations
{
	internal static class StationQueryParser
	{
		// Single source of truth for recognised prefixes, so the parser and the in-game help text can't drift apart.
		public static readonly IReadOnlyList<(string Prefix, SearchField Field, string Help)> Prefixes = new[]
		{
			("country", SearchField.Country, "country:germany"),
			("countrycode", SearchField.CountryCode, "countrycode:gb"),
			("cc", SearchField.CountryCode, "cc:gb (shorthand for countrycode:)"),
			("state", SearchField.State, "state:bavaria"),
			("language", SearchField.Language, "language:english"),
			("lang", SearchField.Language, "lang:english (shorthand for language:)"),
			("tag", SearchField.Tag, "tag:jazz (repeat for more than one)"),
			("codec", SearchField.Codec, "codec:mp3"),
			("bitratemin", SearchField.BitrateMin, "bitratemin:128"),
			("bitratemax", SearchField.BitrateMax, "bitratemax:192"),
		};

		public static SearchQuery Parse(string query)
		{
			SearchQuery result = new SearchQuery();
			if (string.IsNullOrWhiteSpace(query))
				return result;

			foreach (string rawToken in Tokenize(query))
			{
				string token = rawToken;

				bool negate = token.StartsWith("!", StringComparison.Ordinal);
				if (negate)
					token = token.Substring(1);

				SearchField field = SearchField.Name;
				int colon = token.IndexOf(':');
				if (colon > 0)
				{
					string prefix = token.Substring(0, colon);
					SearchField? matched = MatchPrefix(prefix);

					// An unrecognised prefix, or a bare colon inside a plain word, is left as part of the value rather than rejected.
					if (matched.HasValue)
					{
						field = matched.Value;
						token = token.Substring(colon + 1);
					}
				}

				bool exact = token.Length >= 2 && token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal);
				if (exact)
					token = token.Substring(1, token.Length - 2);

				if (token.Length == 0)
					continue;

				result.Conditions.Add(new SearchCondition
				{
					Field = field,
					Value = token,
					Negate = negate,
					Exact = exact,
				});
			}

			return result;
		}

		private static SearchField? MatchPrefix(string prefix)
		{
			foreach ((string candidate, SearchField field, string _) in Prefixes)
			{
				if (string.Equals(candidate, prefix, StringComparison.OrdinalIgnoreCase))
					return field;
			}

			return null;
		}

		// Splits on spaces outside of double-quoted spans, so e.g. tag:"smooth jazz" survives as one token.
		private static IEnumerable<string> Tokenize(string query)
		{
			List<string> tokens = new List<string>();
			StringBuilder current = new StringBuilder();
			bool inQuotes = false;

			foreach (char c in query)
			{
				if (c == '"')
				{
					inQuotes = !inQuotes;
					current.Append(c);
				}
				else if (c == ' ' && !inQuotes)
				{
					if (current.Length > 0)
					{
						tokens.Add(current.ToString());
						current.Clear();
					}
				}
				else
				{
					current.Append(c);
				}
			}

			if (current.Length > 0)
				tokens.Add(current.ToString());

			return tokens;
		}
	}
}