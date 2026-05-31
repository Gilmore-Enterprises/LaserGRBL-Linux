using LaserGRBL;
// Ported from ImageUtilities.cs. System.Drawing replaced with SkiaSharp.
// Bridges the Cyotek.Drawing dithering library (ArgbColor[]) ↔ SkiaBitmap.

using System;
using SkiaSharp;
using LaserGRBL.Imaging;

namespace Cyotek.Drawing.Imaging.ColorReduction
{
	public static class ImageUtilities
	{
		/// <summary>Copy a SkiaBitmap into a new SkiaBitmap (BGRA8888).</summary>
		public static SkiaBitmap Copy(SkiaBitmap source)
		{
			var bmp = source.GetSKBitmap().Copy(SKColorType.Bgra8888);
			return new SkiaBitmap(bmp);
		}

		/// <summary>Convert ArgbColor pixel array back into a SkiaBitmap.</summary>
		public static SkiaBitmap ToBitmap(ArgbColor[] data, CoreSize size)
		{
			var bmp = new SKBitmap(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
			int idx = 0;
			for (int y = 0; y < size.Height; y++)
			for (int x = 0; x < size.Width; x++)
			{
				var c = data[idx++];
				bmp.SetPixel(x, y, new SKColor(c.R, c.G, c.B, c.A));
			}
			return new SkiaBitmap(bmp);
		}

		/// <summary>Extract ARGB pixel array from a SkiaBitmap.</summary>
		internal static ArgbColor[] GetPixelsFrom32BitArgbImage(SkiaBitmap bitmap)
		{
			int w = bitmap.Width, h = bitmap.Height;
			var pixels = new ArgbColor[w * h];
			var skBmp = bitmap.GetSKBitmap();
			int idx = 0;
			for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				SKColor c = skBmp.GetPixel(x, y);
				pixels[idx++] = new ArgbColor(c.Alpha, c.Red, c.Green, c.Blue);
			}
			return pixels;
		}
	}
}
