using System;
using System.Drawing;

namespace JocysCom.ClassLibrary.Drawing
{
	public partial class Effects
	{
		public static Image ResizeWidth(Image image, int width, bool preserveAspect)
		{
			// Calculate new height to maintain aspect ratio.
			int height = image.Height;
			if (preserveAspect)
				height = image.Height * width / image.Width;
			return Resize(image, width, height);
		}

		public static Image ResizeHeight(Image image, int height, bool preserveAspect)
		{
			// Calculate new height to maintain aspect ratio.
			int width = image.Width;
			if (preserveAspect)
				width = image.Width * height / image.Height;
			return Resize(image, width, height);
		}


		public static Image Resize(Image image, int width, int height)
		{
			return Resize(image, width, height, false);
		}

		/// <summary>
		/// If preserve aspect is set then image will be fitted into box with borders.
		/// </summary>
		/// <param name="image"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="preserveAspect"></param>
		/// <returns></returns>
		public static Image Resize(Image image, int width, int height, bool preserveAspect)
		{
			int w = width;
			int h = height;
			if (preserveAspect)
			{
				double oar = image.Width / (double)image.Height;
				double nar = width / (double)height;
				// Left/right borders.
				if (nar < oar)
				{
					w = width;
					h = (int)Math.Round(width / oar, 0);
				}
				// Top/bottom borders.
				else if (nar > oar)
				{
					w = (int)Math.Round(height * oar, 0);
					h = height;
				}
				// Make sure borders have same size.
				w = w + ((width - w) % 2);
				h = h + ((height - h) % 2);
			}
			Bitmap newImage = new Bitmap(w, h, image.PixelFormat);
			newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			Graphics graphics = Basic.GetNewGraphics(newImage);
			// we need to expand image or borders will be transparent.
			// Draw aligned image.
			RectangleF src = new RectangleF(0, 0, image.Width, image.Height);
			RectangleF dst = new RectangleF(0, 0, w, h);
			graphics.DrawImage(image, dst, src, GraphicsUnit.Pixel);
			graphics.Dispose();
			return newImage;
		}

	}
}
