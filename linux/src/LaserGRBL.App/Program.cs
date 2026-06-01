// Avalonia entry point. Replaces the WinForms Program.cs —
// no EnableVisualStyles, no nvapi DllImport, no STAThread magic for COM.
using Avalonia;
using System;
using System.Threading;
using LaserGRBL.Platform;

namespace LaserGRBL
{
	internal static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			// Set version before anything else (LogMessage etc. may read it)
			try { CoreApp.CurrentVersion = typeof(Program).Assembly.GetName().Version ?? new Version(0, 0, 0); }
			catch { CoreApp.CurrentVersion = new Version(0, 0, 0); }

			// Logger (internal; started by logging infrastructure automatically)

			// Parse CLI flags (mirrors WinForms Main)
			foreach (string s in args)
			{
				if (s?.ToLower() == "nogl")
					Settings.ForcedGraphicMode = Settings.GraphicMode.GDI;
				if (s?.ToLower() == "swgl")
					Settings.ForcedGraphicMode = Settings.GraphicMode.DIB;
			}

			// Apply saved UI language
			var ci = Settings.GetObject<System.Globalization.CultureInfo>("User Language", null);
			if (ci != null) Thread.CurrentThread.CurrentUICulture = ci;

			// Load usage stats
			UsageStats.LoadFile();
			CustomButtons.LoadFile();

			// Run Avalonia
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

			// Cleanup (Logger.Stop() is internal — cleanup happens automatically)
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
