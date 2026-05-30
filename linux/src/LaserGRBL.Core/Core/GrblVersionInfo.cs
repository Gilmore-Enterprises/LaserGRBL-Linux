//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// Extracted from GrblCore.cs to break the GrblCommand→GrblCore circular reference.
// GrblCore still has this as a nested class; here it lives at namespace level so Core
// can reference it without pulling in the ~4000-line GrblCore yet.

using System;
using System.Globalization;

namespace LaserGRBL
{
	/// <summary>Identifies the firmware version and vendor of a connected GRBL device.</summary>
	[Serializable]
	public class GrblVersionInfo : IComparable, ICloneable
	{
		int mMajor;
		int mMinor;
		char mBuild;
		bool mOrtur;
		bool mLonger;
		bool mGrblHal;

		string mVendorInfo;
		string mVendorVersion;

		public GrblVersionInfo(int major, int minor, char build, string VendorInfo, string VendorVersion, bool IsHAL)
		{
			mMajor = major; mMinor = minor; mBuild = build;
			mVendorInfo = VendorInfo;
			mVendorVersion = VendorVersion;
			mOrtur = VendorInfo != null && (VendorInfo.Contains("Ortur") || VendorInfo.Contains("Aufero"));
			mLonger = VendorInfo != null && (VendorInfo.Contains("Longer") || VendorInfo.Contains("NanoDuo"));
			mGrblHal = IsHAL;
		}

		public GrblVersionInfo(int major, int minor, char build)
		{ mMajor = major; mMinor = minor; mBuild = build; }

		public GrblVersionInfo(int major, int minor)
		{ mMajor = major; mMinor = minor; mBuild = (char)0; }

		public static bool operator !=(GrblVersionInfo a, GrblVersionInfo b) => !(a == b);

		public static bool operator ==(GrblVersionInfo a, GrblVersionInfo b)
		{
			if (Object.ReferenceEquals(a, null))
				return Object.ReferenceEquals(b, null);
			return a.Equals(b);
		}

		public static bool operator <(GrblVersionInfo a, GrblVersionInfo b)
		{
			if ((object)a == null) throw new ArgumentNullException("a");
			return a.CompareTo(b) < 0;
		}

		public static bool operator <=(GrblVersionInfo a, GrblVersionInfo b)
		{
			if ((object)a == null) throw new ArgumentNullException("a");
			return a.CompareTo(b) <= 0;
		}

		public static bool operator >(GrblVersionInfo a, GrblVersionInfo b) => b < a;
		public static bool operator >=(GrblVersionInfo a, GrblVersionInfo b) => b <= a;

		public override string ToString()
		{
			if (mBuild == (char)0)
				return string.Format("{0}.{1}", mMajor, mMinor);
			else
				return string.Format("{0}.{1}{2}", mMajor, mMinor, mBuild);
		}

		public override bool Equals(object obj)
		{
			GrblVersionInfo v = obj as GrblVersionInfo;
			return v != null && mMajor == v.mMajor && mMinor == v.mMinor && mBuild == v.mBuild
				&& mOrtur == v.mOrtur && mGrblHal == v.mGrblHal;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + mMajor.GetHashCode();
				hash = hash * 23 + mMinor.GetHashCode();
				hash = hash * 23 + mBuild.GetHashCode();
				return hash;
			}
		}

		public int CompareTo(object obj)
		{
			GrblVersionInfo v = obj as GrblVersionInfo;
			if (v == null) return 1;
			if (mMajor != v.mMajor) return mMajor > v.mMajor ? 1 : -1;
			if (mMinor != v.mMinor) return mMinor > v.mMinor ? 1 : -1;
			return mBuild.CompareTo(v.mBuild);
		}

		public object Clone() => MemberwiseClone();

		public int Major => mMajor;
		public int Minor => mMinor;
		public bool IsOrtur => mOrtur;
		public bool IsLonger => mLonger;
		public bool IsHAL => mGrblHal;
		public bool IsLuckyWiFi => (IsOrtur && mVendorInfo == "Ortur Laser Master 3")
			|| (IsLonger && mVendorInfo == "Longer Nano" || mVendorInfo == "NanoDuo");
		public string MachineName => mVendorInfo;

		public string VendorName
		{
			get
			{
				if (mVendorInfo != null && mVendorInfo.ToLower().Contains("ortur"))
					return "Ortur";
				else if (mVendorInfo != null && mVendorInfo.ToLower().Contains("Vigotec"))
					return "Vigotec";
				else if (mBuild == '#')
					return "Emulator";
				else
					return "Unknown";
			}
		}

		public int OrturFWVersionNumber
		{
			get
			{
				try { return int.Parse(mVendorVersion); }
				catch { return -1; }
			}
		}
	}
}
