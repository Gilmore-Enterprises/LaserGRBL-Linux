using System;
using System.Runtime.InteropServices;

namespace LaserGRBL.Platform
{
	/// <summary>
	/// Factory that selects the correct OS implementation at runtime.
	/// Initialised once at startup by the App layer; tests can override individual services.
	/// </summary>
	public static class PlatformServices
	{
		public static ISerialEnumerator Serial    { get; set; } = CreateDefault<ISerialEnumerator>();
		public static ISoundPlayer     Sound      { get; set; } = CreateDefault<ISoundPlayer>();
		public static ISecretStore     Secrets    { get; set; } = CreateDefault<ISecretStore>();
		public static IExternalTool    ExternalTools { get; set; } = new Linux.LinuxExternalTool();
		public static IOsInfo          OsInfo     { get; set; } = CreateDefault<IOsInfo>();
		public static IPathProvider    Paths      { get; set; } = CreateDefault<IPathProvider>();
		public static IFileAssociation FileAssoc  { get; set; } = CreateDefault<IFileAssociation>();

		/// <summary>
		/// Initialise all services for the current OS.
		/// Called once from Program.cs (before the Avalonia AppBuilder runs).
		/// </summary>
		public static void Initialize(string dataPath = null)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Serial   = new Windows.WindowsSerialEnumerator();
				Sound    = new Windows.WindowsSoundPlayer();
				Secrets  = new Windows.WindowsSecretStore();
				OsInfo   = new Linux.LinuxOsInfo(); // pure RuntimeInformation — same on both
				Paths    = dataPath != null ? (IPathProvider)new FixedPathProvider(dataPath)
				                           : new Linux.LinuxPathProvider();
				FileAssoc = new Linux.LinuxFileAssociation(); // no-op on Windows (uses registry separately)
			}
			else
			{
				Serial   = new Linux.LinuxSerialEnumerator();
				Sound    = new Linux.LinuxSoundPlayer();
				Secrets  = new Linux.LinuxSecretStore(dataPath ?? new Linux.LinuxPathProvider().DataPath);
				OsInfo   = new Linux.LinuxOsInfo();
				Paths    = dataPath != null ? (IPathProvider)new FixedPathProvider(dataPath)
				                           : new Linux.LinuxPathProvider();
				FileAssoc = new Linux.LinuxFileAssociation();
			}
			ExternalTools = new Linux.LinuxExternalTool(); // same on both (uses PATH)
		}

		private static T CreateDefault<T>() where T : class => null; // replaced at Initialize()
	}

	/// <summary>Fixed path provider for testing or embedded scenarios.</summary>
	internal class FixedPathProvider : IPathProvider
	{
		public FixedPathProvider(string dataPath) { DataPath = dataPath; AssetPath = AppContext.BaseDirectory; }
		public string DataPath  { get; }
		public string AssetPath { get; }
	}
}
