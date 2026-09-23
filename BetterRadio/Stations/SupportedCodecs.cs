using System;
using System.Collections.Generic;

namespace BetterRadio.Stations
{
	internal static class SupportedCodecs
	{
		public static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"MP3",
		};
	}
}
