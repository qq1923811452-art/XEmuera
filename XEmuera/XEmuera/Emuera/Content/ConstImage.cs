using SkiaSharp;
using System;
using System.Drawing;

namespace MinorShift.Emuera.Content
{
	internal abstract class AbstractImage : AContentFile
	{
		public const int MAX_IMAGESIZE = 8192;
		public SKBitmap Bitmap;
		public virtual SKBitmap GetBitmap() { return Bitmap; }
		public virtual int Width { get { return Bitmap != null ? Bitmap.Width : 0; } }
		public virtual int Height { get { return Bitmap != null ? Bitmap.Height : 0; } }
		protected SKCanvas g;
	}

	/// <summary>
	/// A file-backed image. Resource registration reads only its header;
	/// pixel data is decoded on first use.
	/// </summary>
	internal sealed class ConstImage : AbstractImage
	{
		private readonly object bitmapLock = new object();
		private SKBitmap bitmap;
		private int width;
		private int height;
		private bool metadataLoaded;
		private bool disposed;

		public ConstImage(string name)
		{
			Name = name;
			FilePath = name;
		}

		public readonly string Name;
		public readonly string FilePath;

		public override int Width { get { return width; } }
		public override int Height { get { return height; } }

		/// <summary>
		/// Reads image dimensions without decoding the full pixel buffer.
		/// </summary>
		internal bool TryLoadMetadata()
		{
			lock (bitmapLock)
			{
				if (disposed)
					return false;
				if (metadataLoaded)
					return width > 0 && height > 0;

				try
				{
					using (SKCodec codec = SKCodec.Create(FilePath))
					{
						if (codec == null)
							return false;
						width = codec.Info.Width;
						height = codec.Info.Height;
						metadataLoaded = width > 0 && height > 0;
						return metadataLoaded;
					}
				}
				catch
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Decodes the image only when a renderer first requests its pixels.
		/// The lock prevents duplicate decodes if drawing is requested concurrently.
		/// </summary>
		public override SKBitmap GetBitmap()
		{
			lock (bitmapLock)
			{
				if (disposed || !metadataLoaded)
					return null;
				if (bitmap == null)
				{
					try
					{
						bitmap = SKBitmap.Decode(FilePath);
					}
					catch
					{
						bitmap = null;
					}
				}
				return bitmap;
			}
		}

		public override void Dispose()
		{
			lock (bitmapLock)
			{
				if (disposed)
					return;
				disposed = true;
				if (g != null)
				{
					g.Dispose();
					g = null;
				}
				if (bitmap != null)
				{
					bitmap.Dispose();
					bitmap = null;
				}
			}
			GC.SuppressFinalize(this);
		}

		~ConstImage()
		{
			Dispose();
		}

		public override bool IsCreated
		{
			get { return !disposed && metadataLoaded; }
		}
	}
}
