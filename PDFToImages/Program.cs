using MuPDFLib;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFToImages
{
    class Program
    {
        private static string exe_file_name;
        private static int pdf_file_count = 0;
        private static int pdf_page_count = 0;
        private static int jpg_file_count = 0;
        private static int RenderDPI = 300;
        private static List<string> error_pdf_files = new List<string>();

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                exe_file_name = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                Console.Write(">> PDF to Files Utility from DIGIBOOK 2019/03/27<<\n\n");

                Console.Write(">> 저장할 이미지의 DPI를 입력하세요. : ");
                RenderDPI = int.Parse(Console.ReadLine());
                PDFToImages(args[0]);
                Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                CheckPDFandImages(args[0]);
                Console.Write("\n");
                bool is_error_occured = false;
                if(error_pdf_files.Count() > 0)
                {
                    is_error_occured = true;
                    Console.Write("로딩에 실패한 PDF 파일 ({0})개\n", error_pdf_files.Count());
                    for(int i=0; i<error_pdf_files.Count(); i++)
                    {
                        Console.Write((i+1).ToString() + " : {0}\n", error_pdf_files[i]);
                    }
                }
                if (pdf_page_count != jpg_file_count)
                {
                    is_error_occured = true;
                    Console.Write("PDF Page Count is not same as JPG File Count. ReRun PDFToFiles.\n");
                }

                if (is_error_occured)
                {
                    int code = Console.Read();
                }
            }
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        static void PDFToImages(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                PDFToImages(dir.FullName);
            }
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                foreach (FileInfo file in files)
                {
                    if (file.Extension.ToLower() == ".pdf")
                    {
                        string current_folder = path + "\\" + Path.GetFileNameWithoutExtension(file.Name);
                        DirectoryInfo di = new DirectoryInfo(current_folder);
                        if (di.Exists == false)
                        {
                            di.Create();
                        }
                        MuPDF _mupdf;
                        try
                        {
                            _mupdf = new MuPDF(file.FullName, null);
                            _mupdf.AntiAlias = true;
                            Console.Write("Convert PDF : " + file.FullName + "\n");
                            for (int i = 1; i <= _mupdf.PageCount; i++)
                            {
                                _mupdf.Page = i;
                                string new_file_name = String.Format("{0}\\{1:D4}.jpg", current_folder, i);
                                FileInfo new_file = new FileInfo(new_file_name);
                                if (new_file.Exists == false)
                                {
                                    Bitmap FiratImage = _mupdf.GetBitmap(0, 0, RenderDPI, RenderDPI, 0, RenderType.RGB, false, false, 0);
                                    if (FiratImage != null)
                                    {
                                        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                                        // Create an Encoder object based on the GUID  
                                        // for the Quality parameter category.  
                                        System.Drawing.Imaging.Encoder myEncoder =
                                            System.Drawing.Imaging.Encoder.Quality;

                                        // Create an EncoderParameters object.  
                                        // An EncoderParameters object has an array of EncoderParameter  
                                        // objects. In this case, there is only one  
                                        // EncoderParameter object in the array.  
                                        EncoderParameters myEncoderParameters = new EncoderParameters(1);

                                        EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 85L);
                                        myEncoderParameters.Param[0] = myEncoderParameter;

                                        FiratImage.Save(String.Format("{0}\\{1:D4}.jpg", current_folder, i), jpgEncoder, myEncoderParameters);
                                    }
                                }
                                //Console.SetCursorPosition(0, current_console_line);
                                Console.SetCursorPosition(0, Console.CursorTop);
                                Console.Write(i.ToString() + " / " + _mupdf.PageCount.ToString());
                            }
                            Console.Write("\n");
                        }
                        catch (Exception e)
                        {
                            //Console.Write(e.ToString());
                            Console.Write(" PDF 파일에 이상이 있습니다. 확인이 필요합니다.\n");
                            error_pdf_files.Add(file.FullName);
                            //throw new Pdf2KTException("Error while opening PDF document.", e);
                        }
                    }
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
                    if (file.Extension.ToLower() == ".pdf" && (!error_pdf_files.Contains(file.FullName)))
                    {
                        pdf_file_count++;
                        MuPDF _mupdf;
                        try
                        {
                            _mupdf = new MuPDF(file.FullName, null);
                            _mupdf.AntiAlias = true;
                            pdf_page_count += _mupdf.PageCount;

                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                        }
                        catch (Exception e)
                        {
                            //Console.Write(e.ToString());
                            Console.Write(" PDF 파일에 이상이 있습니다. 확인이 필요합니다.\n");
                            //throw new Pdf2KTException("Error while opening PDF document.", e);
                        }
                    }
                    else if (file.Extension.ToLower() == ".jpg")
                    {
                        jpg_file_count++;
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(String.Format("Check PDF and JPG Count : PDF Files ({0}) , PDF Pages ({1}), JPG Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                    }
                }
            }
        }

        static void PDFToImages2(string path)
        {
            string ext = path.Substring(path.Length - 4, 4).ToLower();
            if (ext == ".pdf")
            {
                FileInfo file = new FileInfo(path);
                int current_console_line = 0;
                //string current_folder = path + "\\" + Path.GetFileNameWithoutExtension(file.Name);
                string current_folder = path.Substring(0, path.Length - 4);
                DirectoryInfo di = new DirectoryInfo(current_folder);
                int RenderDPI = 300;
                if (di.Exists == false)
                {
                    di.Create();
                }
                MuPDF _mupdf;
                try
                {
                    _mupdf = new MuPDF(file.FullName, null);
                    _mupdf.AntiAlias = true;
                    Console.Write("Convert PDF : " + file.FullName + "\n");
                    current_console_line++;
                    for (int i = 1; i <= _mupdf.PageCount; i++)
                    {
                        _mupdf.Page = i;
                        Bitmap FiratImage = _mupdf.GetBitmap(0, 0, RenderDPI, RenderDPI, 0, RenderType.RGB, false, false, 100000000);
                        if (FiratImage != null)
                        {
                            FiratImage.Save(String.Format("{0}\\{1:D4}.jpg", current_folder, i));
                        }
                        //Console.SetCursorPosition(0, current_console_line);
                        Console.Write(i.ToString() + " / " + _mupdf.PageCount.ToString() + "\n");
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
            else
            {
                DirectoryInfo d = new DirectoryInfo(path);

                DirectoryInfo[] dirs = d.GetDirectories();
                foreach (DirectoryInfo dir in dirs)
                {
                    PDFToImages(dir.FullName);
                }
                FileInfo[] files = d.GetFiles();
                if (files.Length > 0)
                {
                    foreach (FileInfo file in files)
                    {
                        if (file.Extension.ToLower() == ".pdf")
                        {
                            PDFToImages(file.FullName);
                            //Process.Start(@exe_file_name, "\"" + file.FullName + "\"");
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
