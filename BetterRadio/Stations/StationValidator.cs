using BetterRadio.Audio;
using BetterRadio.Utilities;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterRadio.Stations
{
	// Manual entry only supports direct MP3 streams for now, so this probes for exactly that rather than
	// reusing the HLS-aware routing in StreamConnection. Follows the same background-thread-plus-polled-fields
	// shape as StationBrowser, for the same reason: no reliable main-thread continuation to await back onto.
	internal class StationValidator
	{
		private const string UserAgent = "BetterRadio";
		private const int ConnectTimeoutMs = 8000;
		private const int ProbeSampleCount = 4096;

		private readonly object _lock = new object();
		private CancellationTokenSource _cts;
		private bool _validating;
		private bool? _success;
		private string _error;

		public bool IsValidating
		{
			get { lock (_lock) return _validating; }
		}

		// null while idle or in progress, then true or false once a probe finishes.
		public bool? Success
		{
			get { lock (_lock) return _success; }
		}

		public string Error
		{
			get { lock (_lock) return _error; }
		}

		public void Reset()
		{
			_cts?.Cancel();

			lock (_lock)
			{
				_validating = false;
				_success = null;
				_error = null;
			}
		}

		public void Validate(string url, Action onSuccess)
		{
			_cts?.Cancel();
			CancellationTokenSource cts = new CancellationTokenSource();
			_cts = cts;

			lock (_lock)
			{
				_validating = true;
				_success = null;
				_error = null;
			}

			Task.Run(() => RunProbe(url, cts.Token, onSuccess), cts.Token);
		}

		private void RunProbe(string url, CancellationToken token, Action onSuccess)
		{
			try
			{
				if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
					throw new FormatException("URL must be a valid http or https address.");

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				request.UserAgent = UserAgent;
				request.Timeout = ConnectTimeoutMs;

				using (WebResponse response = request.GetResponse())
				using (Stream stream = new ForwardOnlyStream(response.GetResponseStream()))
				{
					if (token.IsCancellationRequested) return;

					string contentType = (response.ContentType ?? string.Empty).Split(';')[0].Trim();
					if (!contentType.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase) && !contentType.Equals("audio/mp3", StringComparison.OrdinalIgnoreCase))
						throw new NotSupportedException($"'{contentType}' is not a supported MP3 stream.");

					using (Mp3StreamDecoder decoder = new Mp3StreamDecoder(stream))
					{
						float[] probe = new float[ProbeSampleCount];

						// A handful of decoded samples is enough to confirm this is really MP3 data, not just a matching content type.
						if (decoder.ReadSamples(probe, 0, probe.Length) <= 0)
							throw new InvalidDataException("Connected, but no audio could be decoded.");
					}
				}

				if (token.IsCancellationRequested) return;

				lock (_lock)
				{
					_success = true;
					_validating = false;
					onSuccess();
				}
			}
			catch (Exception ex)
			{
				if (token.IsCancellationRequested) return;

				Logging.LogDebug($"Station validation failed for '{url}': {ex.Message}");

				lock (_lock)
				{
					_success = false;
					_error = ex.Message;
					_validating = false;
				}
			}
		}
	}
}
