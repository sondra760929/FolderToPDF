using MuPDFLib;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFToImages
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                PDFToImages(args[0]);
            }
        }

        static void PDFToImages(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                PDFToImages(dir.FullName);
            }
            int current_console_line = 0;
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLower() == ".pdf")
                    {
                        string current_folder = path + "\\" + Path.GetFileNameWithoutExtension(file.Name);
                        DirectoryInfo di = new DirectoryInfo(current_folder);
                        int RenderDPI = 96;
                        if (di.Exists == false)
                        {
                            di.Create();
                            MuPDF _mupdf;
                            try
                            {
                                _mupdf = new MuPDF(file.FullName, null);
                                _mupdf.AntiAlias = true;
                                Console.Write("Convert PDF : " + file.FullName + "\n");
                                current_console_line++;
                                for (int i=1; i<=_mupdf.PageCount; i++)
                                {
                                    _mupdf.Page = i;
                                    Bitmap FiratImage = _mupdf.GetBitmap(0, 0, RenderDPI, RenderDPI, 0, RenderType.RGB, false, false, 100000000);
                                    if(FiratImage != null)
                                    {
                                        FiratImage.Save(String.Format("{0}\\{1:D4}.jpg", current_folder, i));
                                    }
                                    Console.SetCursorPosition(0, current_console_line);
                                    Console.Write(i.ToString() + " / " + _mupdf.PageCount.ToString());
                                }
                                Console.Write("\n");
                                current_console_line++;
                            }
                            catch (Exception e)
                            {
                                //throw new Pdf2KTException("Error while opening PDF document.", e);
                            }




                            //PdfDocument document = PdfReader.Open(file.FullName);
                            //int imageCount = 0;
                            //// Iterate pages
                            //foreach (PdfPage page in document.Pages)
                            //{
                            //    // Get resources dictionary
                            //    PdfDictionary resources = page.Elements.GetDictionary("/Resources");
                            //    if (resources != null)
                            //    {
                            //        // Get external objects dictionary
                            //        PdfDictionary xObjects = resources.Elements.GetDictionary("/XObject");
                            //        if (xObjects != null)
                            //        {
                            //            ICollection<PdfItem> items = xObjects.Elements.Values;
                            //            // Iterate references to external objects
                            //            foreach (PdfItem item in items)
                            //            {
                            //                PdfReference reference = item as PdfReference;
                            //                if (reference != null)
                            //                {
                            //                    PdfDictionary xObject = reference.Value as PdfDictionary;
                            //                    // Is external object an image?
                            //                    if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image")
                            //                    {
                            //                        ExportImage(xObject, current_folder, ref imageCount);
                            //                    }
                            //                }
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    }
                }
            }
        }

        static void ExportImage(PdfDictionary image, string current_folder, ref int count)
        {
            //string filter = image.Elements.GetName("/Filter");
            //switch (filter)
            //{
            //    case "/DCTDecode":
                    ExportJpegImage(image, current_folder, ref count);
            //        break;

            //    case "/FlateDecode":
            //        ExportAsPngImage(image, ref count);
            //        break;
            //}
        }

        static void ExportJpegImage(PdfDictionary image, string current_folder, ref int count)
        {
            // Fortunately JPEG has native support in PDF and exporting an image is just writing the stream to a file.
            byte[] stream = image.Stream.Value;
            FileStream fs = new FileStream(String.Format("{0}\\{1:D4}.jpeg", current_folder, ++count), FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(stream);
            bw.Close();
        }

        static void ExportAsPngImage(PdfDictionary image, ref int count)
        {
            int width = image.Elements.GetInteger(PdfImage.Keys.Width);
            int height = image.Elements.GetInteger(PdfImage.Keys.Height);
            int bitsPerComponent = image.Elements.GetInteger(PdfImage.Keys.BitsPerComponent);

            // TODO: You can put the code here that converts vom PDF internal image format to a Windows bitmap
            // and use GDI+ to save it in PNG format.
            // It is the work of a day or two for the most important formats. Take a look at the file
            // PdfSharp.Pdf.Advanced/PdfImage.cs to see how we create the PDF image formats.
            // We don't need that feature at the moment and therefore will not implement it.
            // If you write the code for exporting images I would be pleased to publish it in a future release
            // of PDFsharp.
        }
    }
}
