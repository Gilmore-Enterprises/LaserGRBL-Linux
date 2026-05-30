// Avalonia entry point for the Linux port. Replaces the WinForms Program.cs
// (System.Windows.Forms, NVIDIA Optimus nvapi hack, EnableVisualStyles, etc.).
using Avalonia;
using System;

namespace LaserGRBL
{
	internal static class Program
	{
		// Initialization code. Don't use any Avalonia, third-party APIs or any
		// SynchronizationContext-reliant code before AppMain is called.
		[STAThread]
		public static void Main(string[] args) => BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);

		// Avalonia configuration, don't remove; also used by the visual designer.
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace();
	}
}
