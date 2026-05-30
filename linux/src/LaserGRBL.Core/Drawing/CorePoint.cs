using System;

namespace LaserGRBL
{
	/// <summary>
	/// Platform-neutral 2D double-precision point. Replaces System.Windows.Point (WPF/WindowsBase,
	/// Windows-only) in StateBuilder arc calculations. Matches the same constructor signature.
	/// </summary>
	public readonly struct CorePoint : IEquatable<CorePoint>
	{
		public readonly double X;
		public readonly double Y;

		public CorePoint(double x, double y) { X = x; Y = y; }

		public bool Equals(CorePoint other) => X == other.X && Y == other.Y;
		public override bool Equals(object obj) => obj is CorePoint p && Equals(p);
		public override int GetHashCode() => HashCode.Combine(X, Y);
		public override string ToString() => $"({X}, {Y})";

		public static bool operator ==(CorePoint a, CorePoint b) => a.Equals(b);
		public static bool operator !=(CorePoint a, CorePoint b) => !a.Equals(b);
	}
}
