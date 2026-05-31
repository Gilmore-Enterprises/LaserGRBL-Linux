// Factory + adapter that bridges ImageTransform.DitheringMode → Cyotek error-diffusion algorithms
// operating on SkiaBitmap rather than System.Drawing.Bitmap.

using System;
using SkiaSharp;
using LaserGRBL.Imaging;
using Cyotek.Drawing;
using Cyotek.Drawing.Imaging.ColorReduction;

namespace LaserGRBL.Imaging.Dithering
{
	public interface ISkiaDitherer
	{
		SkiaBitmap Apply(SkiaBitmap source);
	}

	public static class DitheringFactory
	{
		public static ISkiaDitherer Create(DitheringMode mode)
		{
			IErrorDiffusion algorithm = mode switch
			{
				DitheringMode.FloydSteinberg      => new FloydSteinbergDithering(),
				DitheringMode.Atkinson            => new AtkinsonDithering(),
				DitheringMode.Burkes              => new BurksDithering(),
				DitheringMode.JarvisJudiceNinke   => new JarvisJudiceNinkeDithering(),
				DitheringMode.Random              => new RandomDithering(),
				DitheringMode.Sierra2             => new Sierra2Dithering(),
				DitheringMode.Sierra3             => new Sierra3Dithering(),
				DitheringMode.SierraLite          => new SierraLiteDithering(),
				DitheringMode.Stucki              => new StuckiDithering(),
				_                                 => null
			};
			return algorithm == null ? null : new CyotekDithererAdapter(algorithm);
		}
	}

	/// <summary>
	/// Adapts a Cyotek IErrorDiffusion algorithm to work on SkiaBitmap pixels.
	/// Matches the original DitherImage() pixel loop from ImageTransform.cs.
	/// </summary>
	internal class CyotekDithererAdapter : ISkiaDitherer
	{
		private readonly IErrorDiffusion _algorithm;

		public CyotekDithererAdapter(IErrorDiffusion algorithm)
			=> _algorithm = algorithm;

		public SkiaBitmap Apply(SkiaBitmap source)
		{
			int w = source.Width, h = source.Height;
			var size = new CoreSize(w, h);

			ArgbColor[] data = ImageUtilities.GetPixelsFrom32BitArgbImage(source);

			for (int row = 0; row < h; row++)
			for (int col = 0; col < w; col++)
			{
				int idx = row * w + col;
				ArgbColor current = data[idx];
				ArgbColor transformed = TransformPixel(current);
				data[idx] = transformed;
				_algorithm.Diffuse(data, current, transformed, col, row, w, h);
			}

			return ImageUtilities.ToBitmap(data, size);
		}

		private static ArgbColor TransformPixel(ArgbColor pixel)
		{
			byte gray = (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
			return gray < 128
				? new ArgbColor(pixel.A, 0, 0, 0)       // black
				: new ArgbColor(pixel.A, 255, 255, 255); // white
		}
	}
}
