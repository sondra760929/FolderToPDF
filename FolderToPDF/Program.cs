using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FolderToPDF
{
	class Program
	{
		static string[] image_format = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".tif", ".bmp", ".pdf" };

		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				FolderToPDF(args[0]);
			}
		}

		static void FolderToPDF(string path)
		{
			DirectoryInfo d = new DirectoryInfo(path);
			FileInfo[] files = d.GetFiles();
			if (files.Length > 0)
			{
				PdfDocument doc = new PdfDocument();
				//PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
				foreach (FileInfo file in files)
				{
					if (image_format.Contains(file.Extension.ToLower()))
					{
						doc.Pages.Add(new PdfPage());
						XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[doc.Pages.Count - 1]);
						XImage img = XImage.FromFile(file.FullName);

						if(img.Size.Width > img.Size.Height)
						{
							doc.Pages[doc.Pages.Count - 1].Orientation = PdfSharp.PageOrientation.Landscape;
						}

						doc.Pages[doc.Pages.Count - 1].Width = XUnit.FromPoint(img.Size.Width);
						doc.Pages[doc.Pages.Count - 1].Height = XUnit.FromPoint(img.Size.Height);

						xgr.DrawImage(img, 0, 0);
					}
				}
				doc.Save(path + ".pdf");
				doc.Close();
			}

			DirectoryInfo[] dirs = d.GetDirectories();
			foreach (DirectoryInfo dir in dirs)
			{
				FolderToPDF(dir.FullName);
			}
		}
	}
}
