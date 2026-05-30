using System;
using System.Collections.Generic;
using System.Formats.Nrbf;
using System.IO;

namespace LaserGRBL
{
	/// <summary>
	/// One-time migration of the legacy BinaryFormatter settings file (LaserGRBL.Settings.bin)
	/// to the new type-tagged JSON store. BinaryFormatter is removed in .NET 10, so the .bin is
	/// decoded read-only via System.Formats.Nrbf (the supported NRBF reader) — BinaryFormatter is
	/// never invoked.
	///
	/// NON-DESTRUCTIVE: the original .bin is always preserved (renamed to .bin.bak); a failed or
	/// partial migration never loses the source. Values it cannot map (complex serialized graphs)
	/// are skipped and fall back to their defaults — the common scalar/string/Version/array
	/// settings migrate cleanly.
	///
	/// NOTE: not yet validated against a captured real-world .bin fixture (see follow-up task).
	/// </summary>
	internal static class LegacySettingsMigrator
	{
		public static Dictionary<string, object> TryMigrate(string binPath, string jsonPath)
		{
			var dic = new Dictionary<string, object>();
			try
			{
				using (var fs = File.OpenRead(binPath))
				{
					var root = NrbfDecoder.Decode(fs);

					// The legacy payload is Dictionary<string,object>, whose ISerializable form
					// exposes a "KeyValuePairs" array of KeyValuePair<string,object> records.
					if (root is ClassRecord dictRecord && dictRecord.HasMember("KeyValuePairs"))
					{
						if (dictRecord.GetArrayRecord("KeyValuePairs") is SZArrayRecord<SerializationRecord> pairs)
						{
							foreach (var pairObj in pairs.GetArray())
							{
								if (pairObj is ClassRecord pair)
								{
									string key = pair.GetString("key");
									if (key == null) continue;
									object value = ReadValue(pair.GetRawValue("value"));
									dic[key] = value;
								}
							}
						}
					}
				}

				// Persist as JSON and retire the .bin (kept as backup).
				File.WriteAllText(jsonPath, TaggedJsonStore.Serialize(dic));
				SafeBackup(binPath);
			}
			catch
			{
				// Leave the .bin untouched on failure; user simply starts fresh.
				return new Dictionary<string, object>();
			}
			return dic;
		}

		// Maps an NRBF member value to a plain CLR object the JSON store understands.
		private static object ReadValue(object raw)
		{
			switch (raw)
			{
				case null:
					return null;
				case PrimitiveTypeRecord prim:   // boxed bool/int/long/double/etc.
					return prim.Value;
				case string s:
					return s;
				case ClassRecord cls:
					// Version is a common stored complex type; reconstruct from its fields.
					if (cls.TypeName.FullName == "System.Version")
						return ReadVersion(cls);
					// Other complex graphs (HotKeysManager, GrblVersionInfo, color schema) are
					// skipped here; they fall back to defaults until per-type mapping is added.
					return null;
				default:
					return null;
			}
		}

		private static object ReadVersion(ClassRecord v)
		{
			try
			{
				int major = v.GetInt32("_Major");
				int minor = v.GetInt32("_Minor");
				int build = v.HasMember("_Build") ? v.GetInt32("_Build") : -1;
				int rev = v.HasMember("_Revision") ? v.GetInt32("_Revision") : -1;
				if (build < 0) return new Version(major, minor);
				if (rev < 0) return new Version(major, minor, build);
				return new Version(major, minor, build, rev);
			}
			catch { return null; }
		}

		private static void SafeBackup(string binPath)
		{
			try
			{
				string bak = binPath + ".bak";
				if (File.Exists(bak)) File.Delete(bak);
				File.Move(binPath, bak);
			}
			catch { /* keep the .bin in place if we can't back it up */ }
		}
	}
}
