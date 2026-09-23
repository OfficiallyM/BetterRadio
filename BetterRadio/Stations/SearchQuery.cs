using System.Collections.Generic;

namespace BetterRadio.Stations
{
	internal class SearchQuery
	{
		internal List<SearchCondition> Conditions { get; } = new List<SearchCondition>();
	}

}
