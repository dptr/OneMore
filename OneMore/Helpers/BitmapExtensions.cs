﻿//************************************************************************************************
// Copyright © 2016 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn
{
	using System;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.IO;
	using System.Runtime.InteropServices.ComTypes;


	internal static class BitmapExtensions
	{
		public static IStream GetReadOnlyStream (this Bitmap bitmap)
		{
			ReadOnlyStream stream = null;

			try
			{
				var memory = new MemoryStream();
				((Image)bitmap).Save(memory, ImageFormat.Png);
				stream = new ReadOnlyStream(memory);
			}
			catch (Exception exc)
			{
				Logger.Current.WriteLine(exc);
			}

			return stream;
		}
	}
}
