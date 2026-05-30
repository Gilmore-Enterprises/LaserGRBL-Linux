using System;

namespace LaserGRBL
{
	/// <summary>
	/// Platform-neutral axis-aligned bounding rectangle. Replaces System.Windows.Rect
	/// (WPF/WindowsBase, Windows-only) used in StateBuilder arc bounding-box calculations.
	/// </summary>
	public struct CoreRect : IEquatable<CoreRect>
	{
		public double X;
		public double Y;
		public double Width;
		public double Height;

		public CoreRect(double x, double y, double width, double height)
		{ X = x; Y = y; Width = width; Height = height; }

		/// <summary>Smallest rect that contains both points (matches System.Windows.Rect(Point,Point)).</summary>
		public CoreRect(CorePoint p1, CorePoint p2)
		{
			X      = Math.Min(p1.X, p2.X);
			Y      = Math.Min(p1.Y, p2.Y);
			Width  = Math.Abs(p1.X - p2.X);
			Height = Math.Abs(p1.Y - p2.Y);
		}

		public double Left   => X;
		public double Top    => Y;
		public double Right  => X + Width;
		public double Bottom => Y + Height;

		public void Union(CoreRect other)
		{
			double left   = Math.Min(Left,   other.Left);
			double top    = Math.Min(Top,    other.Top);
			double right  = Math.Max(Right,  other.Right);
			double bottom = Math.Max(Bottom, other.Bottom);
			X = left; Y = top; Width = right - left; Height = bottom - top;
		}

		public bool Equals(CoreRect other) =>
			X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
		public override bool Equals(object obj) => obj is CoreRect r && Equals(r);
		public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
		public override string ToString() => $"({X},{Y} {Width}×{Height})";

		public static bool operator ==(CoreRect a, CoreRect b) => a.Equals(b);
		public static bool operator !=(CoreRect a, CoreRect b) => !a.Equals(b);
	}
}
