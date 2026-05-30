//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify it under the terms of
// the GPLv3 General Public License as published by the Free Software Foundation; either version 3
// of the License, or (at your option) any later version.

using System;
using System.Collections.Generic;

namespace LaserGRBL
{
	/// <summary>
	/// Application settings store. API-compatible with the original WinForms Settings, but
	/// persisted as type-tagged JSON (LaserGRBL.Settings.json) instead of BinaryFormatter
	/// (removed in .NET 10). A one-time migration from the legacy .bin is applied on first run.
	/// </summary>
	public static class Settings
	{
		private static System.Threading.Timer Timer = new System.Threading.Timer(OnTimerExpire, null, System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
		private static Dictionary<string, object> dic;
		private static string LastCause = null;
		private static readonly object LockString = new object();

		public static bool IsNewFile { get; private set; } = false;
		public static Version PrevVersion { get; private set; } = new Version(0, 0, 0);

		/// <summary>Current app version, written into settings. Set by the App layer at startup
		/// (replaces the WinForms Program.CurrentVersion coupling).</summary>
		public static Version AppVersion { get; set; } =
			System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

		public enum GraphicMode
		{
			AUTO = 0,
			GDI = 1,
			DIB = 2,
			FBO = 3,
		}

		public static GraphicMode ForcedGraphicMode { get; set; } = GraphicMode.AUTO;                            // forced by command line
		public static GraphicMode ConfiguredGraphicMode                                                          // stored in settings
		{
			get { return (GraphicMode)GetObject("ConfiguredGraphicMode", 0); }
			set { SetObject("ConfiguredGraphicMode", (int)value); }
		}
		public static GraphicMode RequestedGraphicMode => ForcedGraphicMode != GraphicMode.AUTO ? ForcedGraphicMode : ConfiguredGraphicMode;
		public static GraphicMode CurrentGraphicMode { get; set; } = GraphicMode.AUTO;                           // actually in use

		private static string JsonFileName => System.IO.Path.Combine(AppPaths.DataPath, "LaserGRBL.Settings.json");
		private static string LegacyBinFileName => System.IO.Path.Combine(AppPaths.DataPath, "LaserGRBL.Settings.bin");

		static Settings()
		{
			try
			{
				string jsonFile = JsonFileName;
				IsNewFile = !System.IO.File.Exists(jsonFile);

				if (!IsNewFile)
				{
					dic = TaggedJsonStore.Deserialize(System.IO.File.ReadAllText(jsonFile));
				}
				else if (System.IO.File.Exists(LegacyBinFileName))
				{
					// One-time migration from the BinaryFormatter-era .bin (see LegacySettingsMigrator).
					dic = LegacySettingsMigrator.TryMigrate(LegacyBinFileName, jsonFile);
					IsNewFile = dic == null || dic.Count == 0;
				}
			}
			catch { }

			if (dic == null)
				dic = new Dictionary<string, object>();

			PrevVersion = GetObject("Current LaserGRBL Version", new Version(0, 0, 0));
			SetObject("Current LaserGRBL Version", AppVersion);
		}

		public static T GetObject<T>(string key, T defval)
		{
			try
			{
				if (ExistObject(key))
				{
					object obj = dic[key];
					if (obj != null && obj.GetType() == typeof(T))
						return (T)obj;
				}
			}
			catch
			{
			}
			return defval;
		}

		public static object GetAndDeleteObject(string key, object defval)
		{
			object rv = ExistObject(key) && dic[key] != null ? dic[key] : defval;
			DeleteObject(key);
			return rv;
		}

		public static void SetObject(string key, object value, bool forcesave = false) //use force-save if calling with a complex object that not support Equal comparison
		{
			if (ExistObject(key))
			{
				bool isdifferent = !Equals(dic[key], value);

				if (value is object[] && dic[key] is object[])
					isdifferent = !ArraysEqual((object[])dic[key], (object[])value);

				dic[key] = value;
				if (isdifferent || forcesave)
					TriggerSave(key);
			}
			else
			{
				dic.Add(key, value);
				TriggerSave(key);
			}
		}

		private static bool ArraysEqual<T>(T[] a1, T[] a2)
		{
			if (ReferenceEquals(a1, a2))
				return true;

			if (a1 == null || a2 == null)
				return false;

			if (a1.Length != a2.Length)
				return false;

			var comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < a1.Length; i++)
			{
				if (!comparer.Equals(a1[i], a2[i])) return false;
			}
			return true;
		}

		private static void TriggerSave(string cause)
		{
			lock (LockString)
			{
				LastCause = cause;
				Timer.Change(2000, System.Threading.Timeout.Infinite);
			}
		}

		public static void Exiting()
		{
			OnTimerExpire(null);
		}

		private static void OnTimerExpire(object state)
		{
			lock (LockString)
			{
				Timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
				InternalSaveFile();
			}
		}

		private static void InternalSaveFile()
		{
			if (LastCause != null)
			{
				System.Diagnostics.Debug.WriteLine($"Save setting file [{LastCause}]");
				try
				{
					System.IO.File.WriteAllText(JsonFileName, TaggedJsonStore.Serialize(dic));
				}
				catch { }
			}
			LastCause = null;
		}

		internal static void DeleteObject(string key)
		{
			if (ExistObject(key))
			{
				dic.Remove(key);
				TriggerSave(key);
			}
		}

		internal static bool ExistObject(string key)
		{
			return dic.ContainsKey(key);
		}
	}
}
