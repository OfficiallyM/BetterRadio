namespace BetterRadio.Utilities
{
	internal static class Logging
	{
		public static void Log(string message, TLDLoader.Logger.LogLevel logLevel = TLDLoader.Logger.LogLevel.Info) =>
			BetterRadio.Instance.Logger.Log(message, logLevel);

		public static void LogDebug(string message)
		{
			if (!BetterRadio.Debug) return;
			BetterRadio.Instance.Logger.LogDebug(message);
		}

		public static void LogInfo(string message) =>
			BetterRadio.Instance.Logger.LogInfo(message);

		public static void LogWarning(string message) =>
			BetterRadio.Instance.Logger.LogWarning(message);

		public static void LogError(string message) =>
			BetterRadio.Instance.Logger.LogError(message);

		public static void LogCritical(string message) =>
			BetterRadio.Instance.Logger.LogCritical(message);
	}
}
