using System;
using System.IO;

namespace LaserGRBL
{
	/// <summary>
	/// Per-user data directory for LaserGRBL. Cross-platform: SpecialFolder.ApplicationData
	/// resolves to %AppData%\LaserGRBL on Windows and ~/.config/LaserGRBL on Linux, matching
	/// the original GrblCore.DataPath behavior. Centralized here so Core has no UI dependency.
	/// </summary>
	public static class AppPaths
	{
		private static string mDataPath;

		public static string DataPath
		{
			get
			{
				if (mDataPath == null)
				{
					mDataPath = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						"LaserGRBL");
					if (!Directory.Exists(mDataPath))
						Directory.CreateDirectory(mDataPath);
				}
				return mDataPath;
			}
			// Test seam: allow tests to redirect the data directory.
			set { mDataPath = value; }
		}
	}
}
