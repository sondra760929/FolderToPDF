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
		static string[] image_format = new string[] { ".jpg", ".jpeg", ".png", ".gif", ".tif", ".bmp"};
        private static int pdf_file_count = 0;
        private static int pdf_page_count = 0;
        private static int jpg_file_count = 0;

        static void Main(string[] args)
		{
			if (args.Length > 0)
			{
                Console.Write(">> Folder To PDF Utility from DIGIBOOK 2019/03/22<<\n\n");
                FolderToPDF(args[0]);
                Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                CheckPDFandImages(args[0]);
                Console.Write("\n");
                if (pdf_page_count != jpg_file_count)
                {
                    Console.Write("PDF Page Count is not same as JPG File Count. ReRun PDFToFiles.\n");
                    int code = Console.Read();
                }
            }
        }

        static void CheckPDFandImages(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                CheckPDFandImages(dir.FullName);
            }
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLower() == ".pdf")
                    {
                        pdf_file_count++;
                        //MuPDF _mupdf;
                        try
                        {
                            PdfDocument doc = new PdfDocument(file.FullName);
                            pdf_page_count += doc.Pages.Count;

                            Console.SetCursorPosition(0, 1);
                            Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                        }
                        catch (Exception e)
                        {
                            //throw new Pdf2KTException("Error while opening PDF document.", e);
                        }
                    }
                    else if (file.Extension.ToLower() == ".jpg")
                    {
                        jpg_file_count++;
                        Console.SetCursorPosition(0, 1);
                        Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                    }
                }
            }
        }

        static void FolderToPDF(string path)
		{
			DirectoryInfo d = new DirectoryInfo(path);
			FileInfo[] files = d.GetFiles();
			if (files.Length > 0)
			{
				PdfDocument doc = new PdfDocument();
                int page_count = 0;
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

                        page_count++;

                    }
				}
                if(page_count > 0)
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
