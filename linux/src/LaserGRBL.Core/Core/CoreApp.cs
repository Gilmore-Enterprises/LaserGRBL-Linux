// Runtime information set by the App entry point (Program.cs / AppBuilder).
// Core accesses version and auto-update state through these static properties
// without depending on the App project.

using System;

namespace LaserGRBL
{
	/// <summary>App-level runtime info set by the App layer at startup.</summary>
	public static class CoreApp
	{
		/// <summary>Application version — set by Program.cs before any Core usage.</summary>
		public static Version CurrentVersion { get; set; } = new Version(0, 0, 0);
	}

	/// <summary>Minimal Program shim so files that reference Program.CurrentVersion compile.</summary>
	public static class Program
	{
		public static Version CurrentVersion => CoreApp.CurrentVersion;
	}

	/// <summary>
	/// Minimal GitHub/auto-update shim for Core references.
	/// The real implementation is in AutoUpdate/GitHub.cs in the App layer.
	/// </summary>
	public static class GitHub
	{
		/// <summary>True while an auto-update install is in progress; UsageStats skips sends.</summary>
		public static bool Updating { get; set; } = false;

		/// <summary>Called at app startup to initialize the update check background task.</summary>
		public static void InitUpdate() { /* App layer overrides */ }
	}
}
