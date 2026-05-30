//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// Extracted verbatim from GrblFile.cs. PointF → CorePoint.

using System;

namespace LaserGRBL
{
	public class ProgramRange
	{
		public class XYRange
		{
			public class Range
			{
				public decimal Min;
				public decimal Max;

				public Range()
				{ ResetRange(); }

				public void UpdateRange(decimal val)
				{
					Min = Math.Min(Min, val);
					Max = Math.Max(Max, val);
				}

				public void ResetRange()
				{
					Min = decimal.MaxValue;
					Max = decimal.MinValue;
				}

				public bool ValidRange
				{ get { return Min != decimal.MaxValue && Max != decimal.MinValue; } }
			}

			public Range X = new Range();
			public Range Y = new Range();

			public void UpdateRange(GrblCommand.Element x, GrblCommand.Element y)
			{
				if (x != null) X.UpdateRange(x.Number);
				if (y != null) Y.UpdateRange(y.Number);
			}

			internal void UpdateRange(double rectX, double rectY, double rectW, double rectH)
			{
				X.UpdateRange((decimal)rectX);
				X.UpdateRange((decimal)(rectX + rectW));
				Y.UpdateRange((decimal)rectY);
				Y.UpdateRange((decimal)(rectY + rectH));
			}

			public void ResetRange()
			{
				X.ResetRange();
				Y.ResetRange();
			}

			public bool ValidRange
			{ get { return X.ValidRange && Y.ValidRange; } }

			public decimal Width
			{ get { return X.Max - X.Min; } }

			public decimal Height
			{ get { return Y.Max - Y.Min; } }

			public CorePoint Center
			{
				get
				{
					if (ValidRange)
						return new CorePoint((double)X.Min + (double)Width / 2.0, (double)Y.Min + (double)Height / 2.0);
					else
						return new CorePoint(0, 0);
				}
			}
		}

		public class SRange
		{
			public class Range
			{
				public decimal Min;
				public decimal Max;

				public Range()
				{ ResetRange(); }

				public void UpdateRange(decimal val)
				{
					Min = Math.Min(Min, val);
					Max = Math.Max(Max, val);
				}

				public void ResetRange()
				{
					Min = decimal.MaxValue;
					Max = decimal.MinValue;
				}

				public bool ValidRange
				{ get { return Min != Max && Min != decimal.MaxValue && Max != decimal.MinValue && Max > 0; } }
			}

			public Range S = new Range();

			public void UpdateRange(decimal s)
			{ S.UpdateRange(s); }

			public void ResetRange()
			{ S.ResetRange(); }

			public bool ValidRange
			{ get { return S.ValidRange; } }
		}

		public XYRange DrawingRange = new XYRange();
		public XYRange MovingRange = new XYRange();
		public SRange SpindleRange = new SRange();

		public void UpdateXYRange(GrblCommand.Element X, GrblCommand.Element Y, bool drawing)
		{
			if (drawing) DrawingRange.UpdateRange(X, Y);
			MovingRange.UpdateRange(X, Y);
		}

		internal void UpdateXYRange(double rectX, double rectY, double rectW, double rectH, bool drawing)
		{
			if (drawing) DrawingRange.UpdateRange(rectX, rectY, rectW, rectH);
			MovingRange.UpdateRange(rectX, rectY, rectW, rectH);
		}

		public void UpdateSRange(GrblCommand.Element S)
		{ if (S != null) SpindleRange.UpdateRange(S.Number); }

		public void ResetRange()
		{
			DrawingRange.ResetRange();
			MovingRange.ResetRange();
			SpindleRange.ResetRange();
		}
	}
}
