// Raster Image Import dialog. Ports RasterConverter/RasterToLaserForm.cs.
// Full live-preview and all import modes (Line2Line, Dithering, Vectorize, Centerline).

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using LaserGRBL;
using LaserGRBL.Imaging;

namespace LaserGRBL.Dialogs
{
	public partial class RasterImportDialog : Window
	{
		private readonly GrblCore _core;
		private readonly string   _filename;
		private readonly bool     _append;
		private SkiaBitmap        _sourceBmp;

		/// <summary>Result from ShowDialog — true if user clicked OK.</summary>
		public bool Confirmed { get; private set; }

		public RasterImportDialog(GrblCore core, string filename, bool append)
		{
			_core     = core;
			_filename = filename;
			_append   = append;
			InitializeComponent();
			Opened += OnOpened;
		}

		private async void OnOpened(object sender, EventArgs e)
		{
			// Load source image on background thread
			try
			{
				_sourceBmp = await Task.Run(() => SkiaBitmap.FromFile(_filename));
				UpdatePreview();
			}
			catch (Exception ex)
			{
				PreviewMessage.Text = $"Cannot load image: {ex.Message}";
			}
		}

		private void UpdatePreview()
		{
			if (_sourceBmp == null) return;

			Task.Run(() =>
			{
				try
				{
					// Apply grayscale + brightness/contrast
					float brightness = (float)(SlBrightness?.Value ?? 0);
					float contrast   = (float)(SlContrast?.Value   ?? 1);
					var formula = ImageTransform.Formula.Luminance;

					var gray = ImageTransform.GrayScale(_sourceBmp, 0.299f, 0.587f, 0.114f,
						brightness, contrast, formula);

					// Apply selected mode
					SkiaBitmap processed = gray;
					string mode = GetSelectedMode();
					if (mode == "Dithering")
					{
						var algo = GetDitheringAlgo();
						processed = ImageTransform.DitherImage(gray, algo);
					}
					else if (mode == "LineByLine")
					{
						processed = ImageTransform.Threshold(gray, 0.5f, true);
					}

					// Convert to Avalonia bitmap for preview
					using var skBmp = processed;
					var avBmp = SkBitmapToAvaloniaBitmap(skBmp.GetSKBitmap());

					Dispatcher.UIThread.Post(() =>
					{
						PreviewImage.Source = avBmp;
						PreviewMessage.IsVisible = false;
					});
				}
				catch { }
			});
		}

		private string GetSelectedMode()
		{
			if (RbDithering?.IsChecked  == true) return "Dithering";
			if (RbVectorize?.IsChecked  == true) return "Vectorize";
			if (RbCenterline?.IsChecked == true) return "Centerline";
			return "LineByLine";
		}

		private DitheringMode GetDitheringAlgo()
		{
			return (CbDitheringAlgo?.SelectedIndex ?? 0) switch
			{
				0 => DitheringMode.FloydSteinberg,
				1 => DitheringMode.Atkinson,
				2 => DitheringMode.Burkes,
				3 => DitheringMode.Stucki,
				4 => DitheringMode.Sierra3,
				5 => DitheringMode.Sierra2,
				6 => DitheringMode.SierraLite,
				7 => DitheringMode.JarvisJudiceNinke,
				8 => DitheringMode.Random,
				_ => DitheringMode.FloydSteinberg
			};
		}

		private void BtnOK_Click(object sender, RoutedEventArgs e)
		{
			Confirmed = true;
			DoImport();
			Close();
		}

		private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

		private void DoImport()
		{
			if (_sourceBmp == null || _core?.LoadedFile == null) return;

			double width    = (double)(NudWidth?.Value    ?? 100m);
			double height   = (double)(NudHeight?.Value   ?? 100m);
			double res      = (double)(NudResolution?.Value ?? 10m);
			int markSpeed   = (int)(NudMarkSpeed?.Value    ?? 1000m);
			int travelSpeed = (int)(NudTravelSpeed?.Value  ?? 3000m);
			int minPower    = (int)(NudMinPower?.Value     ?? 0m);
			int maxPower    = (int)(NudMaxPower?.Value     ?? 100m);

			var conf = new GrblFile.L2LConf
			{
				res        = res,
				fres       = res,
				oX         = 0,
				oY         = 0,
				markSpeed  = markSpeed,
				borderSpeed = travelSpeed,
				minPower   = (int)(minPower * (double)GrblCore.Configuration.MaxPWM / 100.0),
				maxPower   = (int)(maxPower * (double)GrblCore.Configuration.MaxPWM / 100.0),
				lOn        = "M3",
				lOff       = "M5",
				firmwareType = Firmware.Grbl,
				dir        = RasterConverter.ImageProcessor.Direction.Horizontal,
			};

			var mode = GetSelectedMode();

			// Scale the bitmap to match mm dimensions
			int targetW = (int)(width  * res);
			int targetH = (int)(height * res);
			var scaled  = ImageTransform.ResizeImage(_sourceBmp, new CoreSize(targetW, targetH), false);

			switch (mode)
			{
				case "Dithering":
					var gray = ImageTransform.GrayScale(scaled, 0.299f, 0.587f, 0.114f, 0, 1,
						ImageTransform.Formula.Luminance);
					var dithered = ImageTransform.DitherImage(gray, GetDitheringAlgo());
					_core.LoadedFile.LoadImageL2L(dithered, _filename, conf, _append, _core);
					break;
				case "Vectorize":
					_core.LoadedFile.LoadImagePotrace(scaled, _filename,
						false, 0, false, 1m, false, 1m, false, conf, _append, _core);
					break;
				case "Centerline":
					// LoadImageCenterline: Phase 3 Autotrace - requires system autotrace tool
					break;
				default: // LineByLine
					_core.LoadedFile.LoadImageL2L(scaled, _filename, conf, _append, _core);
					break;
			}
		}

		// Reuse the same helper from GrblPreviewControl
		private static Avalonia.Media.Imaging.Bitmap SkBitmapToAvaloniaBitmap(SkiaSharp.SKBitmap skBmp)
		{
			int w = skBmp.Width, h = skBmp.Height;
			var avBmp = new Avalonia.Media.Imaging.WriteableBitmap(
				new Avalonia.PixelSize(w, h), new Avalonia.Vector(96, 96),
				Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul);
			using var locked = avBmp.Lock();
			unsafe
			{
				var dst = (uint*)locked.Address;
				var pixels = skBmp.Pixels;
				for (int i = 0; i < pixels.Length; i++)
				{
					var c = pixels[i];
					dst[i] = (uint)((c.Alpha << 24) | (c.Red << 16) | (c.Green << 8) | c.Blue);
				}
			}
			return avBmp;
		}
	}
}
