using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterRadio.Utilities;

namespace BetterRadio.Stations
{
	internal class StationBrowser
	{
		private const int PageSize = 10;
		private const int UpstreamBatchSize = 50;

		// Stops a codec that is essentially unlisted from causing an unbounded fetch loop.
		private const int MaxUpstreamFetched = 500;

		private readonly RadioBrowserClient _client = new RadioBrowserClient();
		private readonly object _lock = new object();
		private CancellationTokenSource _cts;

		private SearchQuery _query = new SearchQuery();
		private List<BrowseResult> _filtered = new List<BrowseResult>();
		private int _upstreamOffset;
		private bool _upstreamExhausted;
		private int _page;
		private bool _searching;
		private string _error;

		public bool IsSearching
		{
			get { lock (_lock) return _searching; }
		}

		public string Error
		{
			get { lock (_lock) return _error; }
		}

		public IReadOnlyList<BrowseResult> CurrentPage
		{
			get { lock (_lock) return _filtered.Skip(_page * PageSize).Take(PageSize).ToList(); }
		}

		public bool CanGoPrevious
		{
			get { lock (_lock) return _page > 0; }
		}

		// True if a further page is already cached, or the upstream might still have more to offer.
		// Because filtering happens after the fetch, this can still be true right up until a fetch comes back
		// empty, so the next page occasionally turns out empty once fetched.
		public bool CanGoNext
		{
			get
			{
				lock (_lock)
					return _filtered.Count > (_page + 1) * PageSize || !_upstreamExhausted;
			}
		}

		// A new search cancels whichever fetch is still in flight, so a slow query can never overwrite a later one's results.
		public void Search(string rawQuery)
		{
			SearchQuery query = StationQueryParser.Parse(rawQuery);

			_cts?.Cancel();
			CancellationTokenSource cts = new CancellationTokenSource();
			_cts = cts;

			lock (_lock)
			{
				_query = query;
				_filtered = new List<BrowseResult>();
				_upstreamOffset = 0;
				_upstreamExhausted = false;
				_page = 0;
				_searching = true;
				_error = null;
			}

			Task.Run(() => FillPage(cts.Token), cts.Token);
		}

		public void NextPage()
		{
			bool needsFetch;

			lock (_lock)
			{
				if (_filtered.Count > (_page + 1) * PageSize)
				{
					_page++;
					return;
				}

				if (_upstreamExhausted)
					return;

				_page++;
				_searching = true;
				needsFetch = true;
			}

			if (!needsFetch) return;

			_cts?.Cancel();
			CancellationTokenSource cts = new CancellationTokenSource();
			_cts = cts;

			Task.Run(() => FillPage(cts.Token), cts.Token);
		}

		public void PreviousPage()
		{
			lock (_lock)
			{
				if (_page > 0)
					_page--;
			}
		}

		// Keeps fetching upstream batches, filtering to supported codecs, until the current page is full or the upstream is exhausted.
		private void FillPage(CancellationToken token)
		{
			try
			{
				SearchQuery query;
				int neededThrough;

				lock (_lock)
				{
					query = _query;
					neededThrough = (_page + 1) * PageSize;
				}

				while (true)
				{
					int filteredCount;
					bool exhausted;
					int offset;

					lock (_lock)
					{
						filteredCount = _filtered.Count;
						exhausted = _upstreamExhausted;
						offset = _upstreamOffset;
					}

					if (filteredCount >= neededThrough || exhausted || offset >= MaxUpstreamFetched)
						break;

					if (token.IsCancellationRequested)
						return;

					List<Station> batch = _client.Search(query, offset, UpstreamBatchSize);

					if (token.IsCancellationRequested)
						return;

					lock (_lock)
					{
						_upstreamOffset += UpstreamBatchSize;

						if (batch.Count < UpstreamBatchSize)
							_upstreamExhausted = true;

						foreach (Station station in batch)
						{
							if (!SupportedCodecs.Names.Contains(station.Codec)) continue;
							if (string.IsNullOrEmpty(station.UrlResolved)) continue;
							if (!MatchesAll(station, query)) continue;

							_filtered.Add(new BrowseResult(station.Name, station.UrlResolved, station.Codec, station.Bitrate, station.CountryCode));
						}

					}
				}

				lock (_lock)
					_searching = false;
			}
			catch (Exception ex)
			{
				if (token.IsCancellationRequested) return;

				Logging.LogWarning($"Station search for '{_query}' failed: {ex.Message}");

				lock (_lock)
				{
					_error = ex.Message;
					_searching = false;
				}
			}
		}

		private bool MatchesAll(Station station, SearchQuery query)
		{
			foreach (SearchCondition condition in query.Conditions)
			{
				if (!Matches(station, condition))
					return false;
			}

			return true;
		}

		private bool Matches(Station station, SearchCondition condition)
		{
			bool matched;

			switch (condition.Field)
			{
				case SearchField.BitrateMin:
					matched = int.TryParse(condition.Value, out int min) && station.Bitrate >= min;
					break;

				case SearchField.BitrateMax:
					matched = int.TryParse(condition.Value, out int max) && station.Bitrate <= max;
					break;

				default:
					string fieldValue = GetFieldValue(station, condition.Field) ?? string.Empty;
					matched = condition.Exact
						? string.Equals(fieldValue, condition.Value, StringComparison.OrdinalIgnoreCase)
						: fieldValue.IndexOf(condition.Value, StringComparison.OrdinalIgnoreCase) >= 0;
					break;
			}

			return condition.Negate ? !matched : matched;
		}

		private string GetFieldValue(Station station, SearchField field)
		{
			switch (field)
			{
				case SearchField.Name: return station.Name;
				case SearchField.Country: return station.Country;
				case SearchField.CountryCode: return station.CountryCode;
				case SearchField.State: return station.State;
				case SearchField.Language: return station.Language;
				case SearchField.Tag: return station.Tags;
				case SearchField.Codec: return station.Codec;
				default: return null;
			}
		}

	}
}
