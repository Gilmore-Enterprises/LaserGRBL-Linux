// Forward declarations for Phase 3 (Imaging) types used in GrblFile method signatures.
// ICoreBitmap: minimal interface so GrblFile's imaging methods compile with 'object' params.

namespace LaserGRBL
{
	/// <summary>Minimal bitmap interface used in Phase 3 imaging pipeline method signatures.</summary>
	public interface ICoreBitmap
	{
		int Width  { get; }
		int Height { get; }
		/// <summary>Get pixel grayscale value 0-255.</summary>
		int GetPixelGray(int x, int y);
	}
}

// These let Core compile without pulling in RasterConverter / SvgConverter / CsPotrace.
// Phase 3 replaces these stubs with the full implementations.

namespace LaserGRBL.SvgConverter
{
	// ColorFilter enum moved to LaserGRBL.Imaging (the real implementation).
	// Defined here as a type alias using the same underlying int values so
	// Core compiles without referencing Imaging.
	public enum ColorFilter { All = 0, Red = 1, Green = 2, Blue = 3, Black = 4 }

	// Minimal GCodeFromSVG stub — real implementation is in LaserGRBL.Imaging.
	public class GCodeFromSVG
	{
		public int GCodeXYFeed { get; set; }
		public bool UseLegacyBezier { get; set; }
		public bool SvgScaleApply { get; set; }
		public float SvgMaxSize { get; set; }
		public CorePoint UserOffset { get; set; }

		public string convertFromFile(string file, GrblCore core, ColorFilter filter) => "";
		public string convertFromText(string text, GrblCore core) => "";
	}
}

namespace LaserGRBL.RasterConverter
{
	public static class ImageProcessor
	{
		public enum Direction
		{
			None,
			Horizontal, Vertical, Diagonal,
			NewHorizontal, NewVertical, NewDiagonal,
			NewReverseDiagonal, NewGrid, NewDiagonalGrid,
			NewCross, NewDiagonalCross,
			NewSquares, NewZigZag, NewHilbert, NewInsetFilling,
		}
	}
}

namespace CsPotrace
{
	// Minimal stubs — full CsPotrace port happens in Phase 3 (Imaging).
	public class dPoint
	{
		public double x; public double y;
		public dPoint() { }
		public dPoint(double x, double y) { this.x = x; this.y = y; }
		// Properties used in GrblFile arc export
		public double X => x; public double Y => y;
	}

	public class Curve
	{
		// In the real CsPotrace: A and B are scalar dPoints (start/end of curve segment).
		public dPoint A = new dPoint();
		public dPoint B = new dPoint();
		public int n;
		public int[] Tag = new int[0];
	}

	public static class Potrace
	{
		public static void Export2GDIPlus(System.Collections.Generic.List<Curve[]> plist, object g, object b, object p, double inset) { }
	}
}
