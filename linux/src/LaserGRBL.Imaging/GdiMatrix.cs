// GDI+ System.Drawing.Drawing2D.Matrix compatibility shim backed by SkiaSharp.
// Provides the mutable M11/M12/M21/M22/OffsetX/OffsetY API that GCodeFromSVG uses.

using SkiaSharp;
using LaserGRBL;

namespace LaserGRBL.Imaging
{
	/// <summary>
	/// Mutable affine 2D matrix compatible with GDI+ System.Drawing.Drawing2D.Matrix.
	/// Internally uses SKMatrix for the actual computations.
	/// </summary>
	public class GdiMatrix
	{
		// Matrix elements stored as mutable floats (GDI+ uses column-major 3x2)
		public float M11 = 1; // ScaleX
		public float M12 = 0; // SkewY
		public float M21 = 0; // SkewX
		public float M22 = 1; // ScaleY
		public float OffsetX = 0;
		public float OffsetY = 0;

		public GdiMatrix() { }

		public GdiMatrix(float m11, float m12, float m21, float m22, float offsetX, float offsetY)
		{
			M11 = m11; M12 = m12; M21 = m21; M22 = m22; OffsetX = offsetX; OffsetY = offsetY;
		}

		public void SetIdentity() { M11 = 1; M12 = 0; M21 = 0; M22 = 1; OffsetX = 0; OffsetY = 0; }

		private SKMatrix ToSK() => new SKMatrix(M11, M21, OffsetX, M12, M22, OffsetY, 0, 0, 1);

		/// <summary>Transform a point (equivalent to GDI+ Matrix.TransformPoints).</summary>
		public CorePoint MapPoint(CorePoint p)
		{
			float x = M11 * (float)p.X + M21 * (float)p.Y + OffsetX;
			float y = M12 * (float)p.X + M22 * (float)p.Y + OffsetY;
			return new CorePoint(x, y);
		}

		/// <summary>Transform a single SKPoint.</summary>
		public CorePoint MapPoint(SKPoint p)
		{
			float x = M11 * p.X + M21 * p.Y + OffsetX;
			float y = M12 * p.X + M22 * p.Y + OffsetY;
			return new CorePoint(x, y);
		}

		/// <summary>Static multiply: result = a * b.</summary>
		public static GdiMatrix Multiply(GdiMatrix a, GdiMatrix b) => a.Multiply(b);

		/// <summary>Pre-multiply another GdiMatrix onto this one (this = this * other).</summary>
		public GdiMatrix Multiply(GdiMatrix other)
		{
			return new GdiMatrix(
				M11 * other.M11 + M12 * other.M21,
				M11 * other.M12 + M12 * other.M22,
				M21 * other.M11 + M22 * other.M21,
				M21 * other.M12 + M22 * other.M22,
				OffsetX * other.M11 + OffsetY * other.M21 + other.OffsetX,
				OffsetX * other.M12 + OffsetY * other.M22 + other.OffsetY
			);
		}

		public GdiMatrix Clone() => new GdiMatrix(M11, M12, M21, M22, OffsetX, OffsetY);

		/// <summary>Apply scale transformation (equivalent to GDI+ Matrix.Scale).</summary>
		public void Scale(float sx, float sy)
		{
			M11 *= sx; M12 *= sx;
			M21 *= sy; M22 *= sy;
			OffsetX *= sx; OffsetY *= sy;
		}

		/// <summary>Apply translation (equivalent to GDI+ Matrix.Translate).</summary>
		public void Translate(float dx, float dy)
		{
			OffsetX += dx; OffsetY += dy;
		}

		/// <summary>Rotate around a center point given as separate x, y coordinates.</summary>
		public void RotateAt(float angleDegrees, float cx, float cy) => RotateAt(angleDegrees, new CorePoint(cx, cy));

		/// <summary>Rotate around a center point (equivalent to GDI+ Matrix.RotateAt).</summary>
		public void RotateAt(float angleDegrees, CorePoint center)
		{
			Translate(-(float)center.X, -(float)center.Y);
			Rotate(angleDegrees);
			Translate((float)center.X, (float)center.Y);
		}

		/// <summary>Apply rotation in degrees (equivalent to GDI+ Matrix.Rotate).</summary>
		public void Rotate(float angleDegrees)
		{
			float rad = angleDegrees * (float)System.Math.PI / 180f;
			float cos = (float)System.Math.Cos(rad);
			float sin = (float)System.Math.Sin(rad);
			float m11 = M11 * cos + M21 * sin;
			float m12 = M12 * cos + M22 * sin;
			float m21 = -M11 * sin + M21 * cos;
			float m22 = -M12 * sin + M22 * cos;
			M11 = m11; M12 = m12; M21 = m21; M22 = m22;
		}
	}
}
