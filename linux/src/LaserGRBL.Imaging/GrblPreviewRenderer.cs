// G-code preview renderer using SkiaSharp.
// Replaces the GDI+ DrawOnGraphics + DrawJobPreview + DrawJobRange methods
// that are stubbed in GrblFile.cs (Phase 5 stubs).

using System;
using System.Collections.Generic;
using SkiaSharp;
using LaserGRBL;

namespace LaserGRBL.Imaging
{
	/// <summary>
	/// Renders a GrblFile's G-code path into an SKBitmap using SkiaSharp.
	/// The output bitmap is displayed in GrblPreviewControl (Avalonia).
	/// Mirrors the logic of the original GrblPanel.cs / GrblFile.DrawOnGraphics().
	/// </summary>
	public static class GrblPreviewRenderer
	{
		/// <summary>
		/// Render the loaded G-code into a new SKBitmap at the specified size.
		/// Returns null if the file has no valid range.
		/// </summary>
		public static SKBitmap Render(GrblFile file, int width, int height)
		{
			if (file == null || width <= 0 || height <= 0)
				return null;

			var range = file.Range;
			if (!range.MovingRange.ValidRange)
				return null;

			var bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
			using var canvas = new SKCanvas(bmp);

			// Background
			var bgc = ColorScheme.PreviewBackColor;
			canvas.Clear(new SKColor(bgc.R, bgc.G, bgc.B, bgc.A));

			// Compute scale + transform to center the job in the viewport
			var xRange = range.MovingRange.X;
			var yRange = range.MovingRange.Y;
			double jobW = (double)(xRange.Max - xRange.Min);
			double jobH = (double)(yRange.Max - yRange.Min);
			float margin = 20;
			float zoom = jobW > 0 && jobH > 0
				? (float)Math.Min((width - 2 * margin) / jobW, (height - 2 * margin) / jobH)
				: 1f;

			// Origin in screen space: flip Y (G-code Y-up, screen Y-down)
			float ox = margin - (float)xRange.Min * zoom;
			float oy = height - margin + (float)yRange.Min * zoom;

			SKMatrix transform = SKMatrix.CreateScaleTranslation(zoom, -zoom, ox, oy);
			canvas.SetMatrix(transform);

			// Draw G-code paths
			var spb = new GrblCommand.StatePositionBuilder();
			bool firstLine = true;

			foreach (GrblCommand cmd in file)
			{
				try
				{
					cmd.BuildHelper();
					spb.AnalyzeCommand(cmd, false);

					if (spb.TrueMovement())
					{
						CoreColor lineColor = GetLineColor(spb, range.SpindleRange, firstLine);
						byte alpha = lineColor.A;
						if (alpha == 0) { cmd.DeleteHelper(); continue; }

						using var paint = new SKPaint
						{
							Color      = new SKColor(lineColor.R, lineColor.G, lineColor.B, alpha),
							StrokeWidth = 1f / zoom,
							IsStroke   = true,
							IsAntialias = true
						};

						if (!spb.LaserBurning)
						{
							paint.PathEffect = SKPathEffect.CreateDash(new[]{ 2f / zoom, 2f / zoom }, 0);
						}

						if (spb.G0G1 && cmd.IsLinearMovement)
						{
							canvas.DrawLine(
								(float)spb.X.Previous, (float)spb.Y.Previous,
								(float)spb.X.Number,   (float)spb.Y.Number,
								paint);
						}
						else if (spb.G2G3 && cmd.IsArcMovement)
						{
							GrblCommand.G2G3Helper ah = spb.GetArcHelper(cmd);
							if (ah.RectW > 0 && ah.RectH > 0)
							{
								float startDeg = (float)(ah.StartAngle * 180 / Math.PI);
								float sweepDeg = (float)(ah.AngularWidth * 180 / Math.PI);
								var arcRect = new SKRect(
									(float)ah.RectX, (float)ah.RectY,
									(float)(ah.RectX + ah.RectW), (float)(ah.RectY + ah.RectH));
								using var path = new SKPath();
								path.AddArc(arcRect, startDeg, sweepDeg);
								canvas.DrawPath(path, paint);
							}
						}

						firstLine = false;
					}
				}
				catch { }
				finally { cmd.DeleteHelper(); }
			}

			// Draw crosshair at origin
			DrawCrosshair(canvas, zoom, ColorScheme.PreviewCross);

			// Draw job range rectangle (dashed)
			if (range.DrawingRange.ValidRange)
				DrawJobRange(canvas, range, zoom, ColorScheme.PreviewJobRange);

			// Draw ruler ticks
			DrawRuler(canvas, range, zoom, ox, oy, width, height, ColorScheme.PreviewRuler);

			canvas.Flush();
			return bmp;
		}

		private static CoreColor GetLineColor(GrblCommand.StatePositionBuilder spb,
			ProgramRange.SRange sRange, bool firstLine)
		{
			if (firstLine)
				return ColorScheme.PreviewFirstMovement;
			if (spb.LaserBurning)
			{
				// Alpha encodes laser power
				int alpha = spb.GetCurrentAlpha(sRange);
				CoreColor lp = ColorScheme.PreviewLaserPower;
				return CoreColor.FromArgb((byte)alpha, lp.R, lp.G, lp.B);
			}
			return ColorScheme.PreviewOtherMovement;
		}

		private static void DrawCrosshair(SKCanvas canvas, float zoom, CoreColor color)
		{
			float size = 5f / zoom;
			using var paint = new SKPaint
			{
				Color = new SKColor(color.R, color.G, color.B, color.A),
				StrokeWidth = 1.5f / zoom, IsStroke = true, IsAntialias = true
			};
			canvas.DrawLine(-size, 0, size, 0, paint);
			canvas.DrawLine(0, -size, 0, size, paint);
		}

		private static void DrawJobRange(SKCanvas canvas, ProgramRange range, float zoom, CoreColor color)
		{
			float xMin = (float)range.DrawingRange.X.Min;
			float xMax = (float)range.DrawingRange.X.Max;
			float yMin = (float)range.DrawingRange.Y.Min;
			float yMax = (float)range.DrawingRange.Y.Max;
			using var paint = new SKPaint
			{
				Color = new SKColor(color.R, color.G, color.B, color.A),
				StrokeWidth = 1f / zoom, IsStroke = true, IsAntialias = true
			};
			paint.PathEffect = SKPathEffect.CreateDash(new[]{ 3f / zoom, 2f / zoom }, 0);
			canvas.DrawRect(new SKRect(xMin, yMin, xMax, yMax), paint);
		}

		private static void DrawRuler(SKCanvas canvas, ProgramRange range, float zoom,
			float ox, float oy, int width, int height, CoreColor color)
		{
			// Reset to screen coords for ruler
			canvas.SetMatrix(SKMatrix.Identity);
			using var paint = new SKPaint
			{
				Color = new SKColor(color.R, color.G, color.B, 128),
				StrokeWidth = 1, IsStroke = true
			};
			// Horizontal axis line
			canvas.DrawLine(0, oy, width, oy, paint);
			// Vertical axis line
			canvas.DrawLine(ox, 0, ox, height, paint);
		}
	}
}
