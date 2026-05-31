using System;
using System.IO;

namespace LaserGRBL.Platform.Linux
{
	public class LinuxPathProvider : IPathProvider
	{
		// ~/.config/LaserGRBL — same path Environment.SpecialFolder.ApplicationData gives on Linux
		public string DataPath { get; } =
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LaserGRBL");

		// Directory next to the executable (AppImage bundle root or install dir)
		public string AssetPath { get; } = AppContext.BaseDirectory;

		public LinuxPathProvider()
		{
			Directory.CreateDirectory(DataPath);
		}
	}
}
