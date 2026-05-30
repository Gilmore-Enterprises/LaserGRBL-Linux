using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LaserGRBL
{
	/// <summary>
	/// Serializes a heterogeneous Dictionary&lt;string, object&gt; to/from JSON, tagging each
	/// value with its type so it round-trips faithfully. Replaces BinaryFormatter (removed in
	/// .NET 10) for the settings store. Each entry is encoded as { "t": &lt;tag&gt;, "v": &lt;payload&gt; }.
	///
	/// Handled tags: null, bool, int, long, double, dec, str, ver (Version), cul (CultureInfo),
	/// enum (with type), arr (object[]), obj (POCO fallback via System.Text.Json + type name).
	/// </summary>
	internal static class TaggedJsonStore
	{
		private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
		private static readonly JsonSerializerOptions PocoOptions = new JsonSerializerOptions
		{
			IncludeFields = true,
			WriteIndented = false,
		};

		public static string Serialize(Dictionary<string, object> dic)
		{
			var root = new JsonObject();
			foreach (var kv in dic)
				root[kv.Key] = Encode(kv.Value);
			return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
		}

		public static Dictionary<string, object> Deserialize(string json)
		{
			var dic = new Dictionary<string, object>();
			var root = JsonNode.Parse(json) as JsonObject;
			if (root == null)
				return dic;

			foreach (var kv in root)
			{
				try { dic[kv.Key] = Decode(kv.Value as JsonObject); }
				catch { /* skip values that can no longer be resolved (e.g. removed types) */ }
			}
			return dic;
		}

		private static JsonObject Tag(string t, JsonNode v) => new JsonObject { ["t"] = t, ["v"] = v };

		private static JsonNode Encode(object value)
		{
			switch (value)
			{
				case null: return new JsonObject { ["t"] = "null" };
				case bool b: return Tag("bool", b);
				case int i: return Tag("int", i);
				case long l: return Tag("long", l);
				case double d: return Tag("double", JsonValue.Create(d));
				case float f: return Tag("double", JsonValue.Create((double)f));
				case decimal m: return Tag("dec", m.ToString(Inv));
				case string s: return Tag("str", s);
				case Version ver: return Tag("ver", ver.ToString());
				case CultureInfo ci: return Tag("cul", ci.Name);
				case Enum e:
					return new JsonObject
					{
						["t"] = "enum",
						["et"] = e.GetType().AssemblyQualifiedName,
						["v"] = Convert.ToInt64(e, Inv),
					};
				case object[] arr:
					var ja = new JsonArray();
					foreach (var el in arr) ja.Add(Encode(el));
					return new JsonObject { ["t"] = "arr", ["v"] = ja };
				default:
					var type = value.GetType();
					return new JsonObject
					{
						["t"] = "obj",
						["ct"] = type.AssemblyQualifiedName,
						["v"] = JsonSerializer.Serialize(value, type, PocoOptions),
					};
			}
		}

		private static object Decode(JsonObject node)
		{
			if (node == null) return null;
			string tag = (string)node["t"];
			JsonNode v = node["v"];

			switch (tag)
			{
				case "null": return null;
				case "bool": return v.GetValue<bool>();
				case "int": return v.GetValue<int>();
				case "long": return v.GetValue<long>();
				case "double": return v.GetValue<double>();
				case "dec": return decimal.Parse((string)v, Inv);
				case "str": return (string)v;
				case "ver": return Version.Parse((string)v);
				case "cul": return new CultureInfo((string)v);
				case "enum":
				{
					var et = ResolveType((string)node["et"]);
					long raw = v.GetValue<long>();
					return et != null ? Enum.ToObject(et, raw) : null;
				}
				case "arr":
				{
					var src = v as JsonArray;
					var list = new List<object>();
					if (src != null)
						foreach (var el in src) list.Add(Decode(el as JsonObject));
					return list.ToArray();
				}
				case "obj":
				{
					var ct = ResolveType((string)node["ct"]);
					return ct != null ? JsonSerializer.Deserialize((string)v, ct, PocoOptions) : null;
				}
				default:
					return null;
			}
		}

		/// <summary>
		/// Resolves a type from an AssemblyQualifiedName, tolerating assembly-version drift by
		/// retrying against already-loaded assemblies using the full type name.
		/// </summary>
		private static Type ResolveType(string aqn)
		{
			if (string.IsNullOrEmpty(aqn)) return null;

			var t = Type.GetType(aqn, throwOnError: false);
			if (t != null) return t;

			// Strip the assembly-qualified suffix and search loaded assemblies by full name.
			int comma = aqn.IndexOf(',');
			string fullName = comma > 0 ? aqn.Substring(0, comma).Trim() : aqn;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				t = asm.GetType(fullName, throwOnError: false);
				if (t != null) return t;
			}
			return null;
		}
	}
}
