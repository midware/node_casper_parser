using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System;
using SixLabors.ImageSharp.Formats.Jpeg;

using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Diagnostics;
using SixLabors.ImageSharp.Compression;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using Org.BouncyCastle.Utilities.Zlib;

namespace ImageUtil
{
	public class GetSize
	{
		public GetSize(Stream stream)
		{
			using (SixLabors.ImageSharp.Image iOriginal = SixLabors.ImageSharp.Image.Load(stream))
			{
				stream.Position = 0;
				Width = iOriginal.Width;
				Height = iOriginal.Height;
			}
		}

		/// <summary>
		/// The width of the image specified in the class constructor's Stream parameter
		/// </summary>
		public int Width { get; }

		/// <summary>
		/// The height of the image specified in the class constructor's Stream parameter
		/// </summary>
		public int Height { get; }
	}

	static public class Resize
	{
		/// <summary>
		/// Resize and save an image to a Stream specifying its new width and height
		/// </summary>
		public static void SaveImage(Stream imageStream, int newWidth, int newHeight, bool preserveImageRatio, Stream saveToStream, int jpegQuality = 100)
		{
			using (SixLabors.ImageSharp.Image iOriginal = SixLabors.ImageSharp.Image.Load(imageStream))
			{
				imageStream.Position = 0;
				if (preserveImageRatio)
				{
					float percentWidth = newWidth / (float)iOriginal.Width;
					float percentHeight = newHeight / (float)iOriginal.Height;
					float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
					newWidth = (int)Math.Round(iOriginal.Width * percent, 0);
					newHeight = (int)Math.Round(iOriginal.Height * percent, 0);
				}
				resize(imageStream, iOriginal, newWidth, newHeight, saveToStream, jpegQuality);
			}
		}

		/// <summary>
		/// Resize and save an image to a Stream specifying the number of pixels to resize to
		/// </summary>
		public static void SaveImage(Stream imageStream, int newNumberOfPixels, Stream saveToStream, int jpegQuality = 100)
		{
			using (SixLabors.ImageSharp.Image iOriginal = SixLabors.ImageSharp.Image.Load(imageStream))
			{
				imageStream.Position = 0;
				double ratio = Math.Sqrt(newNumberOfPixels / (double)(iOriginal.Width * iOriginal.Height));
				resize(imageStream, iOriginal, (int)Math.Round(iOriginal.Width * ratio, 0), (int)Math.Round(iOriginal.Height * ratio, 0), saveToStream, jpegQuality);
			}
		}

		private static void resize(Stream origSource, SixLabors.ImageSharp.Image image, int newWidth, int newHeight, Stream saveTo, int jpegQuality)
		{          
            image.Mutate(x => x.Resize(newWidth, newHeight));
			//transformImage(image); // NOTE: transform image AFTER resizing it!!!
			var format = SixLabors.ImageSharp.Image.DetectFormat(origSource);
			if (format.Name.ToLower() == "jpeg")
			{
                var encoder = new JpegEncoder
                {
                    Quality = jpegQuality
                };
                image.SaveAsJpeg(saveTo, encoder);
			}
			else
				image.Save(saveTo, format);
		}

        private static ushort GetExifOrientation(/*Image<TPixel> source*/SixLabors.ImageSharp.Image source)
        {
            if (source.Metadata.ExifProfile is null)
            {
                return ExifOrientationMode.Unknown;
            }

            if (!source.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? value))
            {
                return ExifOrientationMode.Unknown;
            }

            ushort orientation;
            if (value.DataType == ExifDataType.Short)
            {
                orientation = value.Value;
            }
            else
            {
                orientation = Convert.ToUInt16(value.Value);
                source.Metadata.ExifProfile.RemoveValue(ExifTag.Orientation);
            }

            source.Metadata.ExifProfile.SetValue(ExifTag.Orientation, ExifOrientationMode.TopLeft);

            return orientation;
        }
        
		/*
		private static void transformImage(SixLabors.ImageSharp.Image image)
		{
			//IExifValue exifOrientation = image.Metadata?.ExifProfile?.GetValue(ExifTag.Orientation);
			 IExifValue exifOrientation = image.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? value);

			//if (exifOrientation == null)
			//	return;

			//var exifOrientation = GetExifOrientation(image);

            RotateMode rotateMode;
			FlipMode flipMode;
			setRotateFlipMode(exifOrientation, out rotateMode, out flipMode);

			image.Mutate(x => x.RotateFlip(rotateMode, flipMode));
			image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)1);
		}*/

		private static void setRotateFlipMode(IExifValue exifOrientation, out RotateMode rotateMode, out FlipMode flipMode)
		{
			var orientation = (ushort)exifOrientation.GetValue();

			switch (orientation)
			{
				case 2:
					rotateMode = RotateMode.None;
					flipMode = FlipMode.Horizontal;
					break;
				case 3:
					rotateMode = RotateMode.Rotate180;
					flipMode = FlipMode.None;
					break;
				case 4:
					rotateMode = RotateMode.Rotate180;
					flipMode = FlipMode.Horizontal;
					break;
				case 5:
					rotateMode = RotateMode.Rotate90;
					flipMode = FlipMode.Horizontal;
					break;
				case 6:
					rotateMode = RotateMode.Rotate90;
					flipMode = FlipMode.None;
					break;
				case 7:
					rotateMode = RotateMode.Rotate90;
					flipMode = FlipMode.Vertical;
					break;
				case 8:
					rotateMode = RotateMode.Rotate270;
					flipMode = FlipMode.None;
					break;
				default:
					rotateMode = RotateMode.None;
					flipMode = FlipMode.None;
					break;
			}
		}
	}
	internal class VerticalPen
	{
		private readonly Rgba32 color;
		public VerticalPen(Rgba32 color)
		{
			this.color = color;
		}
		public void Draw(SixLabors.ImageSharp.Image<Rgba32> bmp, int row, int height)
		{
			if (height <= bmp.Height)
				for (int y = height - 1; y >= 0; y--)
					bmp[row, bmp.Height - 1 - y] = color;
		}
	}


	/*
	static public class Histogram
	{
		/// <summary>
		/// Create a histogram from the data in a stream
		/// </summary>
		static public MemoryStream CreatePNG(Stream stream, int width, int height, LRGB lrgb, byte alphaChannel = 128, bool clipBlackAndWhite = true, byte luminanceShade = 255)
		{
			using (var bmp = SixLabors.ImageSharp.Image<Rgb24>.Load(stream))
			{
				return create(bmp, width, height, lrgb, alphaChannel, clipBlackAndWhite, luminanceShade);
			}
		}

		/// <summary>
		/// Create a histogram from the data in a file
		/// </summary>
		static public MemoryStream CreatePNG(string filename, int width, int height, LRGB lrgb, byte alphaChannel = 128, bool clipBlackAndWhite = true, byte luminanceShade = 255)
		{
			using (var bmp = SixLabors.ImageSharp.Image<Rgb24>.Load(filename))
			{
				return create(bmp, width, height, lrgb, alphaChannel, clipBlackAndWhite, luminanceShade);
			}
		}

		static private MemoryStream create(SixLabors.ImageSharp.Image bmp, int width, int height, LRGB lrgb, byte alpha, bool clip, byte shade)
		{
			ulong[] lumin = new ulong[256];
			ulong[] red = new ulong[256];
			ulong[] green = new ulong[256];
			ulong[] blue = new ulong[256];
			var bred = (lrgb & LRGB.RED) != 0;
			var bgreen = (lrgb & LRGB.GREEN) != 0;
			var bblue = (lrgb & LRGB.BLUE) != 0;
			var blumin = (lrgb == LRGB.LUMINANCE);
			int w = bmp.Width;
			int h = bmp.Height;
			var bmp2 = bmp.CloneAs<Rgb24>();
			for (int y = 0; y < h; y++)
			{
				Span<Rgb24> pixelRow = bmp2.GetPixelRowSpan(y);
				for (int x = 0; x < w; x++)
				{
					var c = pixelRow[x];
					lumin[(int)Math.Round((c.R + c.G + c.B) / 3.0)]++;
					red[c.R]++;
					green[c.G]++;
					blue[c.B]++;
				}
			}
			ulong max = 0;
			int a = (clip ? 1 : 0), b = (clip ? 255 : 256);
			for (int i = a; i < b; i++)
			{
				if (!blumin)
				{
					if (bred)
						if (max < red[i])
							max = red[i];
					if (bgreen)
						if (max < green[i])
							max = green[i];
					if (bblue)
						if (max < blue[i])
							max = blue[i];
				}
				else if (max < lumin[i])
					max = lumin[i];
			}
			double HEIGHTFACTOR = 256.0 / max;
			if (blumin)
			{
				using (var bmplum = new SixLabors.ImageSharp.Image<Rgba32>(256, 256))
				{
					var penlum = new VerticalPen(new Rgba32(shade, shade, shade, alpha));
					for (int i = 0; i < 256; i++)
						penlum.Draw(bmplum, i, (int)(lumin[i] * HEIGHTFACTOR));
					bmplum.Mutate(x => x.Resize(width, height));
					MemoryStream ms = new MemoryStream();
					bmplum.Save(ms, new PngEncoder());
					return ms;
				}
			}
			else
			{
				using (var bmppre = new SixLabors.ImageSharp.Image<Rgba32>(256, 256))
				{
					Image<Rgba32>? bmpred = null, bmpgreen = null, bmpblue = null;
					VerticalPen? penred = null, pengreen = null, penblue = null;
					if (bred)
					{ 
						bmpred = new Image<Rgba32>(256, 256);
						penred = new VerticalPen(new Rgba32(255, 0, 0, alpha));
					}
					if(bgreen)
					{
						bmpgreen = new Image<Rgba32>(256, 256);
						pengreen = new VerticalPen(new Rgba32(0, 255, 0, alpha));
					}
					if(bblue)
					{
						bmpblue = new Image<Rgba32>(256, 256);
						penblue = new VerticalPen(new Rgba32(0, 0, 255, alpha));
					}
					
					for (int i = 0; i < 256; i++)
					{
						if (bred)
							penred.Draw(bmpred, i, (int)(red[i] * HEIGHTFACTOR));
						if (bgreen)
							pengreen.Draw(bmpgreen, i, (int)(green[i] * HEIGHTFACTOR));
						if (bblue)
							penblue.Draw(bmpblue, i, (int)(blue[i] * HEIGHTFACTOR));
					}

					if (bred)
					{
						bmppre.Mutate(x => x.DrawImage(bmpred, 1));
						bmpred.Dispose();
					}
					if (bgreen)
					{
						bmppre.Mutate(x => x.DrawImage(bmpgreen, 1));
						bmpgreen.Dispose();
					}
					if (bblue)
					{
						bmppre.Mutate(x => x.DrawImage(bmpblue, 1));
						bmpblue.Dispose();
					}
					bmppre.Mutate(x => x.Resize(width, height));
					MemoryStream ms = new MemoryStream();
					bmppre.Save(ms, new PngEncoder());
					return ms;
				}
			}
		}
		public enum LRGB
		{
			LUMINANCE = 0,
			RED = 1,
			GREEN = 2,
			BLUE = 4,
			REDBLUE = 1 | 4,
			REDGREEN = 1 | 2,
			BLUEGREEN = 2 | 4,
			REDGREENBLUE = 1 | 2 | 4
		}
	}*/
}
