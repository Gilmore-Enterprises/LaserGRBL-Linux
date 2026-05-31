// Ported from RasterConverter/ImageTransform.cs.
// System.Drawing replaced with SkiaSharp. Same public API surface; callers now use SkiaBitmap.

using System;
using SkiaSharp;

namespace LaserGRBL.Imaging
{
	public enum DitheringMode
	{
		None = 0,
		FloydSteinberg,
		Atkinson,
		Burkes,
		Stucki,
		Sierra3,
		Sierra2,
		SierraLite,
		JarvisJudiceNinke,
		Random
	}

	public static class ImageTransform
	{
		// ─── Resize ────────────────────────────────────────────────────────────

		/// <summary>
		/// Resize a SkiaBitmap to a new size, optionally clearing the alpha channel.
		/// Replaces the GDI+ ResizeImage(Image, Size, bool, InterpolationMode).
		/// </summary>
		public static SkiaBitmap ResizeImage(SkiaBitmap source, CoreSize targetSize, bool killAlpha, int interpolationQuality = 1)
		{
			var dst = SkiaBitmap.Create(targetSize.Width, targetSize.Height);
			using var canvas = new SKCanvas(dst.GetSKBitmap());
			canvas.Clear(killAlpha ? SKColors.White : SKColors.Transparent);
			var srcRect = new SKRect(0, 0, source.Width, source.Height);
			var dstRect = new SKRect(0, 0, targetSize.Width, targetSize.Height);
			canvas.DrawBitmap(source.GetSKBitmap(), srcRect, dstRect,
				new SKPaint { IsAntialias = interpolationQuality > 0, FilterQuality = SKFilterQuality.High });

			return dst;
		}

		// ─── Threshold ─────────────────────────────────────────────────────────

		/// <summary>Convert to 1-bit black/white via threshold (0.0–1.0).</summary>
		public static SkiaBitmap Threshold(SkiaBitmap source, float threshold, bool apply)
		{
			int w = source.Width, h = source.Height;
			var dst = SkiaBitmap.Create(w, h);
			using var canvas = new SKCanvas(dst.GetSKBitmap());
			canvas.Clear(SKColors.White);

			var srcBmp = source.GetSKBitmap();
			var dstBmp = dst.GetSKBitmap();
			int thresh = (int)(threshold * 255);

			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				SKColor c = srcBmp.GetPixel(x, y);
				int lum = (c.Red * 299 + c.Green * 587 + c.Blue * 114) / 1000;
				dstBmp.SetPixel(x, y, lum < thresh ? SKColors.Black : SKColors.White);
			}
			return dst;
		}

		// ─── Invert ────────────────────────────────────────────────────────────

		public static SkiaBitmap InvertingImage(SkiaBitmap source)
		{
			int w = source.Width, h = source.Height;
			var dst = SkiaBitmap.Create(w, h);
			var srcBmp = source.GetSKBitmap();
			var dstBmp = dst.GetSKBitmap();
			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				SKColor c = srcBmp.GetPixel(x, y);
				dstBmp.SetPixel(x, y, new SKColor((byte)(255 - c.Red), (byte)(255 - c.Green), (byte)(255 - c.Blue), c.Alpha));
			}
			return dst;
		}

		// ─── Grayscale + brightness/contrast ──────────────────────────────────

		public enum Formula { Luminance, Average, MaxChannel }

		public static SkiaBitmap GrayScale(SkiaBitmap source, float R, float G, float B,
			float brightness, float contrast, Formula formula)
		{
			int w = source.Width, h = source.Height;
			var dst = SkiaBitmap.Create(w, h);
			var srcBmp = source.GetSKBitmap();
			var dstBmp = dst.GetSKBitmap();

			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				SKColor c = srcBmp.GetPixel(x, y);
				float gray = formula switch
				{
					Formula.Luminance   => c.Red * R + c.Green * G + c.Blue * B,
					Formula.Average     => (c.Red + c.Green + c.Blue) / 3f,
					Formula.MaxChannel  => Math.Max(c.Red, Math.Max(c.Green, c.Blue)),
					_                   => c.Red * R + c.Green * G + c.Blue * B
				};
				// Apply brightness (offset) and contrast (scale around midpoint)
				gray = (gray - 128) * contrast + 128 + brightness;
				gray = Math.Clamp(gray, 0, 255);
				byte g = (byte)gray;
				dstBmp.SetPixel(x, y, new SKColor(g, g, g, c.Alpha));
			}
			return dst;
		}

		// ─── Whitenize (remove near-white pixels) ────────────────────────────

		public static SkiaBitmap Whitenize(SkiaBitmap source, int threshold, bool demo)
		{
			int w = source.Width, h = source.Height;
			var dst = SkiaBitmap.Create(w, h);
			var srcBmp = source.GetSKBitmap();
			var dstBmp = dst.GetSKBitmap();
			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				SKColor c = srcBmp.GetPixel(x, y);
				int lum = (c.Red * 299 + c.Green * 587 + c.Blue * 114) / 1000;
				if (lum >= threshold)
					dstBmp.SetPixel(x, y, demo ? new SKColor(255, 200, 200, 255) : SKColors.White);
				else
					dstBmp.SetPixel(x, y, c);
			}
			return dst;
		}

		// ─── Rotate/Flip ──────────────────────────────────────────────────────

		public static SkiaBitmap RotateFlipY(SkiaBitmap source)
		{
			// Equivalent to RotateNoneFlipY — flip vertically
			var srcBmp = source.GetSKBitmap();
			var dstBmp = srcBmp.Copy();
			using var surface = new SKCanvas(dstBmp);
			surface.Scale(1, -1, 0, source.Height / 2f);
			surface.DrawBitmap(srcBmp, 0, 0);
			return new SkiaBitmap(dstBmp);
		}

		// ─── Dithering dispatcher ─────────────────────────────────────────────

		/// <summary>Apply error-diffusion dithering to a grayscale SkiaBitmap.</summary>
		public static SkiaBitmap DitherImage(SkiaBitmap source, DitheringMode mode)
		{
			var ditherer = Dithering.DitheringFactory.Create(mode);
			if (ditherer == null) return source; // DitheringMode.None
			return ditherer.Apply(source);
		}
	}
}
