// Avalonia custom control that displays the G-code preview.
// Replaces WinForms GrblPanel (2D GDI+) using SkiaSharp → Avalonia.Media.Imaging.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Runtime.InteropServices;
using SkiaSharp;
using LaserGRBL;
using LaserGRBL.Imaging;

namespace LaserGRBL
{
	/// <summary>
	/// Displays a rendered G-code preview. Backed by a SkiaSharp offscreen bitmap
	/// that is re-rendered when the GrblFile or viewport changes.
	/// Replaces both the WinForms GrblPanel (2D) and — for Phase 5 — provides the
	/// baseline before the full OpenGL panel (GrblPanel3D) is ported.
	/// </summary>
	public class GrblPreviewControl : Control
	{
		private GrblFile _file;
		private SKBitmap _skBitmap;
		private Avalonia.Media.Imaging.Bitmap _avaloniaBmp;
		private bool _dirty = true;

		// Mouse pan state
		private Point? _lastPan;

		public GrblPreviewControl()
		{
			ClipToBounds = true;
			PointerPressed  += OnPointerPressed;
			PointerMoved    += OnPointerMoved;
			PointerReleased += OnPointerReleased;
		}

		/// <summary>Set the GrblFile to preview. Triggers a re-render.</summary>
		public void SetFile(GrblFile file)
		{
			_file = file;
			Invalidate();
		}

		/// <summary>Mark the preview as dirty so it re-renders on next paint.</summary>
		public void Invalidate()
		{
			_dirty = true;
			Dispatcher.UIThread.Post(InvalidateVisual);
		}

		public override void Render(DrawingContext context)
		{
			int w = (int)Bounds.Width;
			int h = (int)Bounds.Height;
			if (w <= 0 || h <= 0) return;

			// Background
			var bg = ColorScheme.PreviewBackColor;
			context.FillRectangle(
				new SolidColorBrush(new Avalonia.Media.Color(bg.A, bg.R, bg.G, bg.B)),
				new Rect(0, 0, w, h));

			if (_file == null || !_file.Range.MovingRange.ValidRange)
			{
				// No file loaded — just show the background
				return;
			}

			// Re-render into SKBitmap if dirty or size changed
			if (_dirty || _skBitmap == null || _skBitmap.Width != w || _skBitmap.Height != h)
			{
				_skBitmap?.Dispose();
				_avaloniaBmp?.Dispose();

				_skBitmap   = GrblPreviewRenderer.Render(_file, w, h);
				_avaloniaBmp = _skBitmap != null ? SkBitmapToAvalonia(_skBitmap) : null;
				_dirty = false;
			}

			if (_avaloniaBmp != null)
				context.DrawImage(_avaloniaBmp, new Rect(0, 0, w, h));
		}

		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);
			if (change.Property == BoundsProperty) Invalidate();
		}

		// ─── Mouse pan (matches GrblPanel mouse dragging) ─────────────────────

		private void OnPointerPressed(object sender, PointerPressedEventArgs e)
		{
			if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
				_lastPan = e.GetPosition(this);
		}

		private void OnPointerMoved(object sender, PointerEventArgs e)
		{
			// Future: pan the preview viewport
		}

		private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			_lastPan = null;
		}

		// ─── SKBitmap → Avalonia.Media.Imaging.Bitmap conversion ─────────────

		private static Avalonia.Media.Imaging.Bitmap SkBitmapToAvalonia(SKBitmap skBmp)
		{
			try
			{
				// Get pixel data from SKBitmap
				var pixels = skBmp.Pixels;
				int w = skBmp.Width, h = skBmp.Height;

				// SKBitmap pixels are BGRA; Avalonia WriteableBitmap expects BGRA too
				var avBmp = new WriteableBitmap(
					new PixelSize(w, h),
					new Vector(96, 96),
					Avalonia.Platform.PixelFormat.Bgra8888,
					Avalonia.Platform.AlphaFormat.Premul);

				using var locked = avBmp.Lock();
				unsafe
				{
					var dst = (uint*)locked.Address;
					for (int i = 0; i < pixels.Length; i++)
					{
						var c = pixels[i];
						// SKColor is ARGB; we need BGRA in memory
						dst[i] = (uint)((c.Alpha << 24) | (c.Red << 16) | (c.Green << 8) | c.Blue);
					}
				}
				return avBmp;
			}
			catch
			{
				return null;
			}
		}
	}
}
