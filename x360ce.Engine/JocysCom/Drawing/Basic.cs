using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace JocysCom.ClassLibrary.Drawing
{

	public class Basic
	{

		/// <summary>
		/// Capture screen image.
		/// </summary>
		public static Bitmap CaptureImage(int x, int y, int w, int h)
		{
			var b = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			using (var g = Graphics.FromImage(b))
			{
				g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
			}
			return b;
		}

#if NETSTANDARD // .NET Standard
#else // .NET Framework

		/// <summary>
		/// Take screenshot.
		/// </summary>
		public static Bitmap CaptureImage(System.Windows.Forms.Screen screen = null)
		{
			var s = screen ?? System.Windows.Forms.Screen.PrimaryScreen;
			return CaptureImage(
				s.Bounds.X, s.Bounds.Y,
				s.Bounds.Width, s.Bounds.Height
			);
		}

		public static void CaptureImage(ref Bitmap b, System.Windows.Forms.Screen screen = null)
		{
			var s = screen ?? System.Windows.Forms.Screen.PrimaryScreen;
			CaptureImage(ref b,
				s.Bounds.X, s.Bounds.Y,
				s.Bounds.Width, s.Bounds.Height
			);
		}

#endif

		public static void CaptureImage(ref Bitmap b, int x, int y, int w, int h)
		{
			var format = System.Drawing.Imaging.PixelFormat.Format24bppRgb;
			if (b is null || b.Width != w || b.Height != h || b.PixelFormat != format)
				b = new Bitmap(w, h, format);
			using (var g = Graphics.FromImage(b))
			{
				g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
			}
		}

		/// <summary>
		///  Bitmap bytes have to be created via a direct memory copy of the bitmap
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public static byte[] ImageToBytes(Bitmap b)
		{
			var converter = System.ComponentModel.TypeDescriptor.GetConverter(b);
			var bytes = (byte[])converter.ConvertTo(b, typeof(byte[]));
			return bytes;
		}

		/// <summary>
		/// Bitmap bytes have to be created using Image.Save()
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static Image BytesToImage(byte[] bytes)
		{
			var ms = new System.IO.MemoryStream(bytes);
			var image = Image.FromStream(ms);
			// Do NOT close the stream!
			return image;
		}

		/// <summary>
		/// Set array of RGB/ARGB bytes to image.
		/// </summary>
		public static void SetImageBytes(Bitmap b, byte[] source)
		{
			var r = new Rectangle(0, 0, b.Width, b.Height);
			var data = b.LockBits(r,
				System.Drawing.Imaging.ImageLockMode.WriteOnly,
				b.PixelFormat);
			var destination = data.Scan0;
			// Maximum length of array.
			var maxLength = data.Stride * b.Height;
			var length = source.Length;
			var sourceIndex = 0;
			System.Runtime.InteropServices.Marshal.Copy(source, sourceIndex, destination, length);
			b.UnlockBits(data);
		}

		/// <summary>
		/// Get color bytes. Native bitmap color byte order for 32bpp: [B,G,R,A...], 24bpp: [B,G,R...].
		/// </summary>
		/// <param name="destination">Allow to supply array buffer in case </param>
		/// <param name="count">Number of bytes to get</param>
		public static byte[] GetImageBytes(Bitmap b, int? count = null)
		{
			byte[] bytes = null;
			GetImageBytes(ref bytes, b, count);
			return bytes;
		}

		/// <summary>
		/// Get color bytes. Native bitmap color byte order for 32bpp: [B,G,R,A...], 24bpp: [B,G,R...].
		/// </summary>
		/// <param name="destination">Allow to supply array buffer in case </param>
		/// <param name="count">Number of bytes to get</param>
		public static void GetImageBytes(ref byte[] destination, Bitmap b, int? count = null)
		{
			var r = new Rectangle(0, 0, b.Width, b.Height);
			var data = b.LockBits(r,
				System.Drawing.Imaging.ImageLockMode.ReadOnly,
				b.PixelFormat);
			var source = data.Scan0;
			// Don't use data.Stride to get bytes per line, because it is rounded
			// up to the nearest multiple of 4 to keep sensible memory alignment.
			// Calculate maximum length of array.
			int depth = Image.GetPixelFormatSize(b.PixelFormat);
			int bytesPerPixel = depth / 8;
			var maxLength = b.Width * b.Height * bytesPerPixel;
			var length = count ?? maxLength;
			if (destination is null)
				destination = new byte[length];
			if (destination.Length != length)
				Array.Resize(ref destination, length);
			var destinationIndex = 0;
			System.Runtime.InteropServices.Marshal.Copy(source, destination, destinationIndex, length);
			b.UnlockBits(data);
		}

		public static Color[] BytesToColors(byte[] bytes, bool useAlpha = false)
		{
			var ps = useAlpha ? 4 : 3;
			var mod = bytes.Length % ps;
			var pixels = (bytes.Length - mod) / ps;
			if (mod > 0)
				pixels++;
			var colors = new Color[pixels];
			for (int p = 0; p < pixels; p++)
			{
				var b = 0;
				var B = (p * ps + b < bytes.Length) ? bytes[p * ps + b] : (byte)0; b++;
				var G = (p * ps + b < bytes.Length) ? bytes[p * ps + b] : (byte)0; b++;
				var R = (p * ps + b < bytes.Length) ? bytes[p * ps + b] : (byte)0; b++;
				var A = (byte)0xFF;
				if (useAlpha)
					A = (p * ps + b < bytes.Length) ? bytes[p * ps + b] : (byte)0; b++;
				colors[p] = Color.FromArgb(A, R, G, B);
			}
			return colors;
		}

		/// <summary>
		/// Convert integer colors to bytes. Native bitmap color byte order for 24bpp: [B,G,R...], 32bpp: [B,G,R,A...].
		/// </summary>
		/// <param name="colors"></param>
		/// <param name="useAlpha"></param>
		/// <returns></returns>
		public static byte[] ColorsToBytes(int[] colors, bool useAlpha = false)
		{
			var ps = useAlpha ? 4 : 3;
			var bytes = new byte[colors.Length * ps];
			for (int i = 0; i < colors.Length; i++)
			{
				var c = colors[i];
				//var x = c.ToString("X6");
				bytes[i * ps + 0] = (byte)(c & 0xFF);
				bytes[i * ps + 1] = (byte)(c >> 8 & 0xFF);
				bytes[i * ps + 2] = (byte)(c >> 16 & 0xFF);
				if (useAlpha)
					bytes[i * ps + 3] = (byte)(c >> 24 & 0xFF);
			}
			return bytes;
		}

		/// <summary>
		/// Reverse [A,R,G,B...] byte order to [B,G,R,A...] or [R,G,B...] to [B,G,R...]
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="useAlpha"></param>
		public static void ReverseColorByteOrder(byte[] bytes, bool useAlpha = false)
		{
			var ps = useAlpha ? 4 : 3;
			var mod = bytes.Length % ps;
			var pixels = (bytes.Length - mod) / ps;
			for (int p = 0; p < pixels; p += ps)
				Array.Reverse(bytes, p * ps, ps);
		}

		/// <summary>
		/// Convert 3 byte [B,G,R...] array to 4 byte [B,G,R,A...] array.
		/// </summary>
		public static byte[] BppFrom24To32Bit(byte[] bytes, byte alpha = 0xFF)
		{
			var mod = bytes.Length % 3;
			var pixels = (bytes.Length - mod) / 3;
			if (mod > 0)
				pixels++;
			var result = new byte[pixels * 4];
			byte b;
			for (int i = 0; i < pixels; i++)
			{
				b = 0;
				result[i * 4 + b] = (i * 3 + b < bytes.Length) ? bytes[i * 3 + b] : (byte)0; b++;
				result[i * 4 + b] = (i * 3 + b < bytes.Length) ? bytes[i * 3 + b] : (byte)0; b++;
				result[i * 4 + b] = (i * 3 + b < bytes.Length) ? bytes[i * 3 + b] : (byte)0; b++;
				result[i * 4 + b] = alpha; b++;
			}
			return result;
		}

		public static bool SaveToFile(string fileName, Image image, bool createFolder = false)
		{
			return SaveToFile(fileName, image, 100L, createFolder);
		}

		public static bool SaveToFile(string fileName, Image image, long quality, bool createFolder = false)
		{
			var file = new System.IO.FileInfo(fileName);
			if (createFolder && !file.Directory.Exists)
				file.Directory.Create();
			switch (file.Extension)
			{
				case ".jpg":
				case ".jpeg":
					var encoder = ImageCodecInfo.GetImageEncoders()[1];
					var encoderParameters = new EncoderParameters(1);
					encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
					image.Save(fileName, encoder, encoderParameters);
					break;
				case ".png":
					// Png is special. You can't use the Bitmap Save() method with a "non-seekable" stream. Some image formats require that the stream can seek.
					// SUPPRESS: CWE-73: External Control of File Name or Path
					// Note: False Positive. File path is not externally controlled by the user.
					var fs = System.IO.File.OpenWrite(file.FullName);
					image.Save(fs, ImageFormat.Png);
					fs.Flush();
					fs.Close();
					break;
				case ".gif":
					image.Save(file.FullName, ImageFormat.Gif);
					break;
				case ".bmp":
					image.Save(file.FullName, ImageFormat.Bmp);
					break;
				default:
					break;
			}
			return true;
		}


		public static Image GetFromText(Graphics graphics, string text, int opacity, string fontFamilyName, float fontSize, Color color, FontStyle fontStyle)
		{
			// Create variable to put temporary images.
			// Create the Font object for the image text drawing.
			var font = new Font(fontFamilyName, fontSize, fontStyle, GraphicsUnit.Point);
			// Set best quality rendering type.
			//graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			//graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var format = StringFormat.GenericDefault;
			// Measure the text size before render (unscaled, unwrapped) and with maximum width of the string in pixels (no word wrap).
			// Returns a SizeF structure that represents the size, in pixels, of the string specified in the text parameter as drawn with the font parameter and the stringFormat parameter.
			var size = graphics.MeasureString(text, font, int.MaxValue, format);
			// Calculate new image size in pixels.
			int width = (int)Math.Ceiling(size.Width);
			int height = (int)Math.Ceiling(size.Height);
			// Create empty Image based on supplied size.
			var image = new Bitmap(width, height);
			// Create graphics based on image.
			var g = Graphics.FromImage(image);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			// Modify Alpha (transparency) value.
			var alphaValue = (double)opacity / 100 * 255;
			int alpha = (int)(Math.Round(alphaValue, 0));
			if (alpha >= 255) alpha = 255;
			if (alpha <= 0) alpha = 0;
			// Create the brush based on the color and alpha.
			var brush = new SolidBrush(Color.FromArgb(alpha, color));
			// Draw text
			var rect = new RectangleF(0, 0, width, height);
			g.DrawString(text, font, brush, rect, format);
			font.Dispose();
			brush.Dispose();
			return image;
		}

		// Get image location by size.

		public static Point AlignedPosition(SizeF size, SizeF backSize, ContentAlignment alignment)
		{

			// Determine draw area based on placement
			float y = 0;
			float x = 0;
			switch (alignment)
			{
				case ContentAlignment.TopLeft: // =1
					y = 0;
					x = 0;
					break;
				case ContentAlignment.TopCenter: // =2
					y = 0;
					x = (backSize.Width - size.Width) / 2F;
					break;
				case ContentAlignment.TopRight: // =4
					y = 0;
					x = (backSize.Width - size.Width);
					break;
				case ContentAlignment.MiddleLeft: // =16  huh?  where's 8?
					y = (backSize.Height - size.Height) / 2F;
					x = 0;
					break;
				case ContentAlignment.MiddleCenter: // =32
					y = (backSize.Height - size.Height) / 2F;
					x = (backSize.Width - size.Width) / 2F;
					break;
				case ContentAlignment.MiddleRight: //=64
					y = (backSize.Height - size.Height) / 2F;
					x = (backSize.Width - size.Width);
					break;
				case ContentAlignment.BottomLeft: // =256  and 128?;
					y = (backSize.Height - size.Height);
					x = 0;
					break;
				case ContentAlignment.BottomCenter: // =512
					y = (backSize.Height - size.Height);
					x = (backSize.Width - size.Width) / 2F;
					break;
				case ContentAlignment.BottomRight: // =1024
					y = (backSize.Height - size.Height);
					x = (backSize.Width - size.Width);
					break;
				default:
					;
					x = 0;
					y = 0;
					break;
			}
			return new Point((int)Math.Round(x, 0), (int)Math.Round(y, 0));
		}

		public static Graphics GetNewGraphics(Image image)
		{
			var graphics = Graphics.FromImage(image);
			graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
			return graphics;
		}

		/// <summary>
		/// Convert indexed format to 32 bit or you will get exception when creating Graphics object.
		/// Exception: A Graphics object cannot be created from an image that has an indexed pixel format.
		/// </summary>
		/// <param name="original"></param>
		/// <returns></returns>
		public static Bitmap ConvertToRGB(Bitmap original)
		{
			var tmp = new Bitmap(original.Width, original.Height);
			tmp.SetResolution(original.HorizontalResolution, original.VerticalResolution);
			var g = Graphics.FromImage(tmp);
			g.DrawImageUnscaled(original, 0, 0);
			g.DrawImage(original, new Rectangle(0, 0, tmp.Width, tmp.Height), 0, 0, tmp.Width, tmp.Height, GraphicsUnit.Pixel);
			g.Dispose();
			return tmp;
		}

		public static bool IsIndexed(Bitmap image)
		{
			switch (image.PixelFormat)
			{
				case PixelFormat.Undefined:
				case PixelFormat.Format16bppArgb1555:
				case PixelFormat.Format16bppGrayScale:
				case PixelFormat.Format1bppIndexed:
				case PixelFormat.Format4bppIndexed:
				case PixelFormat.Format8bppIndexed:
				case PixelFormat.Indexed:
					return true;
			}
			return false;
		}

	}
}
