using System;
using System.IO;
using SkiaSharp;

namespace LaserGRBL.Imaging
{
	/// <summary>
	/// Concrete ICoreBitmap backed by SkiaSharp's SKBitmap.
	/// Used everywhere GrblFile.LoadImageL2L / LoadImagePotrace / GetColor work with pixel data.
	/// </summary>
	public sealed class SkiaBitmap : ICoreBitmap, IDisposable
	{
		private SKBitmap _bmp;
		private bool _disposed;

		public SkiaBitmap(SKBitmap bmp)
		{
			_bmp = bmp ?? throw new ArgumentNullException(nameof(bmp));
		}

		public int Width  => _bmp.Width;
		public int Height => _bmp.Height;

		/// <summary>
		/// Returns grayscale intensity (0 = white/no burn, 255 = black/full burn).
		/// Matches the original GDI+ formula: (255 - R) * A / 255.
		/// </summary>
		public int GetPixelGray(int x, int y)
		{
			SKColor c = _bmp.GetPixel(x, y);
			// Luminance from RGB, then invert (laser-off is white = 0, laser-on is black = 255)
			int lum = (int)(0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue);
			int inverted = 255 - lum;
			// Apply alpha: transparent = no burn
			return inverted * c.Alpha / 255;
		}

		public SKBitmap GetSKBitmap() => _bmp;

		// ─── Factory methods ──────────────────────────────────────────────────

		/// <summary>Load from file (PNG, JPEG, BMP, GIF — all formats SkiaSharp supports).</summary>
		public static SkiaBitmap FromFile(string path)
		{
			using var stream = File.OpenRead(path);
			var bmp = SKBitmap.Decode(stream)
				?? throw new InvalidOperationException($"SkiaSharp could not decode: {path}");
			// Normalise to ARGB_8888 (same as original GDI+ pixel format)
			if (bmp.ColorType != SKColorType.Bgra8888)
			{
				var converted = bmp.Copy(SKColorType.Bgra8888);
				bmp.Dispose();
				bmp = converted;
			}
			return new SkiaBitmap(bmp);
		}

		/// <summary>Load from in-memory byte array.</summary>
		public static SkiaBitmap FromBytes(byte[] data)
		{
			var bmp = SKBitmap.Decode(data)
				?? throw new InvalidOperationException("SkiaSharp could not decode image bytes");
			return new SkiaBitmap(bmp);
		}

		/// <summary>Create a blank bitmap (white, fully opaque).</summary>
		public static SkiaBitmap Create(int width, int height)
		{
			var bmp = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
			using var canvas = new SKCanvas(bmp);
			canvas.Clear(SKColors.White);
			return new SkiaBitmap(bmp);
		}

		/// <summary>Save as PNG to a file path.</summary>
		public void SavePng(string path)
		{
			using var image = SKImage.FromBitmap(_bmp);
			using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
			using var fs    = File.Create(path);
			data.SaveTo(fs);
		}

		/// <summary>Save as PNG and return the bytes.</summary>
		public byte[] EncodePng()
		{
			using var image = SKImage.FromBitmap(_bmp);
			using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
			return data.ToArray();
		}

		public void Dispose()
		{
			if (!_disposed) { _bmp.Dispose(); _disposed = true; }
		}
	}
}
