// Adapter that wraps SKPath with GDI+ GraphicsPath-compatible methods
// used by PotraceClipper to build Clipper polygon inputs.

using System.Collections.Generic;
using SkiaSharp;
using LaserGRBL;

namespace LaserGRBL.Imaging
{
	/// <summary>
	/// Wraps SKPath with the GDI+ GraphicsPath API surface needed by PotraceClipper.
	/// Specifically: AddLine, CubicTo (4-point), Flatten, PathPoints.
	/// </summary>
	public class PotracePathAdapter
	{
		private readonly SKPath _path = new SKPath();
		private List<CorePoint> _flattenedPoints;

		// ─── GDI+ compatibility methods ──────────────────────────────────────

		public void AddLine(CorePoint a, CorePoint b)
		{
			_path.MoveTo((float)a.X, (float)a.Y);
			_path.LineTo((float)b.X, (float)b.Y);
			_flattenedPoints = null;
		}

		/// <summary>Add a cubic Bézier (matches GDI+ AddBezier signature: p1, c1, c2, p2).</summary>
		public void CubicTo(CorePoint p1, CorePoint c1, CorePoint c2, CorePoint p2)
		{
			_path.MoveTo((float)p1.X, (float)p1.Y);
			_path.CubicTo((float)c1.X, (float)c1.Y, (float)c2.X, (float)c2.Y, (float)p2.X, (float)p2.Y);
			_flattenedPoints = null;
		}

		/// <summary>Flatten the path into line segments (approximates GDI+ GraphicsPath.Flatten()).</summary>
		public void Flatten()
		{
			_flattenedPoints = new List<CorePoint>();
			// Use _path directly for iteration
			using var iter = _path.CreateRawIterator();
			var pts = new SKPoint[4];
			SKPathVerb verb;
			SKPoint current = default;
			while ((verb = iter.Next(pts)) != SKPathVerb.Done)
			{
				switch (verb)
				{
					case SKPathVerb.Move:
						current = pts[0];
						_flattenedPoints.Add(new CorePoint(current.X, current.Y));
						break;
					case SKPathVerb.Line:
						current = pts[1];
						_flattenedPoints.Add(new CorePoint(current.X, current.Y));
						break;
					case SKPathVerb.Cubic:
						// Approximate cubic with line segments (tolerance ~1px)
						var cubic = new SKPath();
						cubic.MoveTo(pts[0]);
						cubic.CubicTo(pts[1], pts[2], pts[3]);
						for (float t = 0.1f; t <= 1.0f; t += 0.1f)
						{
							// Lerp approximation
							float x = BezierPoint(pts[0].X, pts[1].X, pts[2].X, pts[3].X, t);
							float y = BezierPoint(pts[0].Y, pts[1].Y, pts[2].Y, pts[3].Y, t);
							_flattenedPoints.Add(new CorePoint(x, y));
						}
						current = pts[3];
						break;
				}
			}
		}

		/// <summary>Points after Flatten(). Matches GDI+ GraphicsPath.PathPoints.</summary>
		public IReadOnlyList<CorePoint> PathPoints => _flattenedPoints ?? new List<CorePoint>();

		private static float BezierPoint(float p0, float p1, float p2, float p3, float t)
		{
			float u = 1 - t;
			return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
		}

		public void Dispose() => _path.Dispose();
	}
}
