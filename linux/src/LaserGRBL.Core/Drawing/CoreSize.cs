using System;

namespace LaserGRBL
{
	/// <summary>
	/// Platform-neutral 2D size (integer). Replaces System.Drawing.Size in GrblFile draw helpers.
	/// </summary>
	public readonly struct CoreSize : IEquatable<CoreSize>
	{
		public readonly int Width;
		public readonly int Height;

		public CoreSize(int width, int height) { Width = width; Height = height; }

		public bool Equals(CoreSize other) => Width == other.Width && Height == other.Height;
		public override bool Equals(object obj) => obj is CoreSize s && Equals(s);
		public override int GetHashCode() => HashCode.Combine(Width, Height);
		public override string ToString() => $"{Width}×{Height}";

		public static bool operator ==(CoreSize a, CoreSize b) => a.Equals(b);
		public static bool operator !=(CoreSize a, CoreSize b) => !a.Equals(b);
	}

	/// <summary>Float-precision size. Replaces System.Drawing.SizeF.</summary>
	public readonly struct CoreSizeF : IEquatable<CoreSizeF>
	{
		public readonly float Width;
		public readonly float Height;

		public CoreSizeF(float width, float height) { Width = width; Height = height; }

		public bool Equals(CoreSizeF other) => Width == other.Width && Height == other.Height;
		public override bool Equals(object obj) => obj is CoreSizeF s && Equals(s);
		public override int GetHashCode() => HashCode.Combine(Width, Height);
		public override string ToString() => $"{Width}×{Height}";

		public static bool operator ==(CoreSizeF a, CoreSizeF b) => a.Equals(b);
		public static bool operator !=(CoreSizeF a, CoreSizeF b) => !a.Equals(b);
	}
}
