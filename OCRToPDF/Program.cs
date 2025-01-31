﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PDFjet.NET;
using Font = PDFjet.NET.Font;
using HPdf;
using System.Diagnostics;
using System.Net.Mail;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
//using UglyToad.PdfPig;
//using UglyToad.PdfPig.Content;

namespace FolderToPDF
{
    class FieldData
    {
        public List<Point> points = new List<Point>();
        public string text;
        public bool line_break;
    }

    class PrevOcrData
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
        public string line;
    }
    class Program
    {
        static string[] image_format = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };
        //private static int pdf_file_count = 0;
        //private static int pdf_page_count = 0;
        //private static int jpg_file_count = 0;
        private static int ocr_processing_page_count = 0;
        private static int pdf_save_count = 0;
        static string accessKey = "RFV6Q1RPSnRlbElzelljdHRoeEVUWlZaZGRFWnZuT0U=";//"bnBrbU5NRWJBakZYTnRJTWJoY1NpV1ZwenFEYllEaHM=";
        static string uriBase = "https://bd61323638684688a1e648de03a65c1f.apigw.ntruss.com/custom/v1/12350/9b3ed5ef657408042e07bb6deb64648dd11faf84671df3bdb5bb9d19dc463da8/general";//"https://43d0993faa7143c0add4cbdfa7fac53d.apigw.ntruss.com/custom/v1/12325/800a9d7344b09c6a7ea43f71842cf3473eb134405af99c384a5a5c8c204b39d2/general";

        private static bool ocr_all = false;
        private static List<int> ocr_pages = new List<int>();

        private static MailAddress sendAddress = new MailAddress("qnrtmzos@gmail.com");
        private static MailAddress toAddress = new MailAddress("ceo@docuscan.co.kr");
        private static string sendPassword = "jwfqpcndndqyanwj";
        static float size_ratio = 210.0f / 876.0f;

        private static List<string> error_list = new List<string>();
        public static string ReadPassword(char mask)
        {
            const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
            int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

            var pass = new Stack<char>();
            char chr = (char)0;

            while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
            {
                if (chr == BACKSP)
                {
                    if (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == CTRLBACKSP)
                {
                    while (pass.Count > 0)
                    {
                        System.Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (FILTERED.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push((char)chr);
                    System.Console.Write(mask);
                }
            }

            System.Console.WriteLine();

            return new string(pass.Reverse().ToArray());
        }

        /// <summary>
        /// Like System.Console.ReadLine(), only with a mask.
        /// </summary>
        /// <returns>the string the user typed in </returns>
        public static string ReadPassword()
        {
            return ReadPassword('*');
        }
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                System.IO.FileInfo fi = new System.IO.FileInfo("C:/Program Files/DIGIBOOK/OCRToPDF/naver.txt");
                if (fi.Exists)
                {
                    string[] lines = System.IO.File.ReadAllLines("C:/Program Files/DIGIBOOK/OCRToPDF/naver.txt");
                    if (lines.Length == 2)
                    {
                        accessKey = lines[0];
                        uriBase = lines[1];
                    }
                }

                Console.Write(">> OCR To PDF Utility from DIGIBOOK 2022/02/18<<\n\n");

                Console.Write("암호를 입력하세요. : ");
                string input = ReadPassword();
                Console.Write(input);

                if (input != "ceohwang")
                {
                    Console.Write("Wrong Password.\n");
                    int code = Console.Read();
                    return;
                }

                Console.Write("처리 방식을 선택하세요. [all / 1 / 1-15] : ");
                input = Console.ReadLine();

                if (input.ToLower().Contains("all"))
                {
                    ocr_all = true;
                }
                else
                {
                    ocr_all = false;
                    string[] pages = input.Split(',');
                    for (int i = 0; i < pages.Length; i++)
                    {
                        int index = pages[i].IndexOf('-');
                        if (index > 0)
                        {
                            int start = 0;
                            int end = 0;
                            if (int.TryParse(pages[i].Substring(0, index), out start))
                            {
                                if (int.TryParse(pages[i].Substring(index + 1, pages[i].Length - index - 1), out end))
                                {
                                    if (end > start)
                                    {
                                        for (int j = start; j <= end; j++)
                                        {
                                            ocr_pages.Add(j);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            index = pages[i].IndexOf('~');
                            if (index > 0)
                            {
                                int start = 0;
                                int end = 0;
                                if (int.TryParse(pages[i].Substring(0, index), out start))
                                {
                                    if (int.TryParse(pages[i].Substring(index + 1, pages[i].Length - index - 1), out end))
                                    {
                                        if (end > start)
                                        {
                                            for (int j = start; j <= end; j++)
                                            {
                                                ocr_pages.Add(j);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                int start = 0;
                                if (int.TryParse(pages[i], out start))
                                {
                                    ocr_pages.Add(start);
                                }
                            }
                        }
                    }
                }

                if (ocr_all || ocr_pages.Count > 0)
                {
                    FolderToPDF_Progressing(args[0]);
                    string mail_content = "";
                    mail_content = "OCR 처리 페이지 : " + ocr_processing_page_count.ToString() + "\n\n" + "PDF 생성 파일 개수 : " + pdf_save_count.ToString();
                    SendEMail(DateTime.Now.ToString("yyyy-MM-dd HHmmss") + " OCR to PDF 결과 리포트", mail_content);

                    if(error_list.Count > 0)
                    {
                        string path_error = args[0] + "/00000.오류/";
                        DirectoryInfo d_ocr = new DirectoryInfo(path_error);
                        if (d_ocr.Exists == false)
                        {
                            d_ocr.Create();
                        }

                        for(int i=0; i<error_list.Count; i++)
                        {
                            string path_error_dir = path_error + error_list[i];
                            DirectoryInfo d_error = new DirectoryInfo(path_error_dir);
                            if (d_error.Exists == false)
                            {
                                d_error.Create();
                            }
                        }
                    }
                }
                //            Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
                //Console.Clear();
                //CheckPDFandImages(args[0]);
                //            Console.Write("\n");
                //            if (pdf_page_count != jpg_file_count)
                //            {
                //                Console.Write("PDF Page Count is not same as Image File Count. ReRun PDFToFiles.\n");
                //                int code = Console.Read();
                //            }
            }
        }

        static bool SendEMail(string subobject, string body)
        {
            SmtpClient smtp = null;
            MailMessage message = null;
            try
            {
                smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(sendAddress.Address, sendPassword),
                    Timeout = 20000
                };
                message = new MailMessage(sendAddress, toAddress)
                {
                    Subject = subobject,
                    Body = body
                };
                smtp.Send(message);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            finally
            {
                if (smtp != null)
                    smtp.Dispose();
                if (message != null)
                    message.Dispose();
            }
        }
        static List<FieldData> GetFields(string json)
        {
            List<FieldData> return_value = new List<FieldData>();

            JObject root = JObject.Parse(json);
            if (root.Count > 0)
            {
                JArray images = (JArray)root["images"];
                if (images != null)
                {
                    for (int i = 0; i < images.Count; i++)
                    {
                        JObject image = (JObject)images[i];
                        if (image != null && image.Count > 0)
                        {
                            JValue inferResult = (JValue)image["inferResult"];
                            if (inferResult != null && inferResult.ToString() == "SUCCESS")
                            {
                                JArray fields = (JArray)image["fields"];
                                for (int j = 0; j < fields.Count; j++)
                                {
                                    JObject field = (JObject)fields[j];
                                    if (field.Count > 0)
                                    {
                                        FieldData fd = new FieldData();
                                        JObject boundingPoly = (JObject)field["boundingPoly"];
                                        if (boundingPoly.Count > 0)
                                        {
                                            JArray vertices = (JArray)boundingPoly["vertices"];
                                            for (int k = 0; k < vertices.Count; k++)
                                            {
                                                Point p = new Point(int.Parse(vertices[k]["x"].ToString()), int.Parse(vertices[k]["y"].ToString()));
                                                fd.points.Add(p);
                                            }
                                        }
                                        JValue inferText = (JValue)field["inferText"];
                                        fd.text = inferText.ToString();

                                        JValue lineBreak = (JValue)field["lineBreak"];
                                        fd.line_break = (bool)lineBreak.Value;

                                        return_value.Add(fd);
                                    }
                                }
                            }
                            else
                            {
                                //	ocr error
                            }
                        }
                    }
                }
            }

            return return_value;
        }
        //static void CheckPDFandImages(string path)
        //{
        //	DirectoryInfo d = new DirectoryInfo(path);

        //	DirectoryInfo[] dirs = d.GetDirectories();
        //	foreach (DirectoryInfo dir in dirs)
        //	{
        //		CheckPDFandImages(dir.FullName);
        //	}
        //	FileInfo[] files = d.GetFiles();
        //	if (files.Length > 0)
        //	{
        //		foreach (FileInfo file in files)
        //		{
        //			if (file.Extension.ToLower() == ".pdf")
        //			{
        //				pdf_file_count++;
        //				//MuPDF _mupdf;
        //				try
        //				{
        //					PdfDocument doc = new PdfDocument(file.FullName);
        //					pdf_page_count += doc.Pages.Count;

        //					Console.SetCursorPosition(0, 1);
        //					Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
        //				}
        //				catch (Exception e)
        //				{
        //					//throw new Pdf2KTException("Error while opening PDF document.", e);
        //				}
        //			}
        //			else if (image_format.Contains(file.Extension.ToLower()))
        //			{
        //				jpg_file_count++;
        //				Console.SetCursorPosition(0, 1);
        //				Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
        //			}
        //		}
        //	}
        //}

        static float GetValueWithRatio(int value)
        {
            return GetValueWithRatio((float)value);
        }
        static float GetValueWithRatio(float value)
        {
            return (value * size_ratio);
        }

        static void page_TextOut(HPdfPage page, float page_height, HPdfFont kr_font, string text_out, float left, float right, float top, float bottom)
        {
            float actual_font_size = Math.Abs(bottom - top);
            float font_ratio = 0.75f;
            float actual_width = Math.Abs(right - left);
            float real_width = 0;
            bool is_ok = false;
            while (is_ok == false)
            {
                if (actual_font_size * font_ratio > 600)
                {
                    font_ratio -= 0.02f;
                }
                else
                {
                    page.SetFontAndSize(kr_font, actual_font_size * font_ratio);
                    page.MeasureText(text_out, actual_width, false, ref real_width);
                    if (real_width > actual_width)
                    {
                        //	써보니 큼.
                        font_ratio -= 0.02f;
                    }
                    else
                    {
                        is_ok = true;
                    }
                }
            }
            float x_offset = (actual_width - real_width) / 2.0f;
            float y_offset = (actual_font_size * (1.0f - font_ratio)) / 2.0f;
            page.TextOut(left + x_offset, page_height - bottom + y_offset, text_out);
        }

        static void FolderToPDF_Progressing(string path)
        {
            DirectoryInfo d = new DirectoryInfo(path);
            FileInfo[] files = d.GetFiles();
            if (files.Length > 0)
            {
                try
                {
                    Console.Write(">> 폴더 검색 : " + path);
                    List<FileInfo> ocr_files = new List<FileInfo>();
                    List<FileInfo> image_files = new List<FileInfo>();
                    int current_page = 0;
                    //	1. 대상 파일 수집, 개수 설정
                    foreach (FileInfo file in files)
                    {
                        if (image_format.Contains(file.Extension.ToLower()))
                        {
                            String fileName = file.FullName;
                            if (file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg" || file.Extension.ToLower() == ".png")
                            {
                                current_page++;
                                image_files.Add(file);

                                if (ocr_all || ocr_pages.Contains(current_page))
                                {
                                    ocr_files.Add(file);
                                }
                            }
                        }
                    }
                    Console.Write(string.Format(" >> 이미지[{0}], OCR[{1}] 파일 선택\n", image_files.Count, ocr_files.Count));

                    if (image_files.Count > 0)
                    {
                        string path_ocr = path + "/ocr/";
                        string error_string = "";
                        int error_count = 0;
                        int sucesses_count = 0;
                        Dictionary<int, Dictionary<int, List<PrevOcrData>>> prev_ocr_datas = new Dictionary<int, Dictionary<int, List<PrevOcrData>>>();
                        bool use_prev_ocr = false;
                        string prev_ocr_file = path + "_ocr.txt";
                        string prev_pie_file = path + ".txt";

                        int current_block = 0;
                        string save_text_value = "";
                        string save_text_line = "";
                        string save_origin_text_value = "";
                        string save_ocr_text_value = "";

                        System.IO.FileInfo fi = new System.IO.FileInfo(prev_pie_file);
                        if (fi.Exists)
                        {
                            //  한번 ocr 처리를 한 파일
                            fi = new System.IO.FileInfo(prev_ocr_file);
                            if (fi.Exists)
                            {
                                //  신규 ocr 처리를 한 파일
                                use_prev_ocr = true;
                                string[] lines = System.IO.File.ReadAllLines(prev_ocr_file, Encoding.Default);
                                for (int i = 0; i < lines.Length; i++)
                                {
                                    string[] line_split = lines[i].Split(',');
                                    if (line_split.Length > 6)
                                    {
                                        PrevOcrData prevOcrData = new PrevOcrData();
                                        int page = int.Parse(line_split[0]);
                                        int block = int.Parse(line_split[1]);
                                        prevOcrData.left = float.Parse(line_split[2]);
                                        prevOcrData.right = float.Parse(line_split[3]);
                                        prevOcrData.top = float.Parse(line_split[4]);
                                        prevOcrData.bottom = float.Parse(line_split[5]);
                                        prevOcrData.line = line_split[6];
                                        for (int j = 7; j < line_split.Length; j++)
                                        {
                                            prevOcrData.line += ",";
                                            prevOcrData.line += line_split[j];
                                        }

                                        if (!prev_ocr_datas.ContainsKey(page))
                                        {
                                            prev_ocr_datas.Add(page, new Dictionary<int, List<PrevOcrData>>());
                                        }

                                        if(!prev_ocr_datas[page].ContainsKey(block))
                                        {
                                            prev_ocr_datas[page].Add(block, new List<PrevOcrData>());
                                        }
                                        prev_ocr_datas[page][block].Add(prevOcrData);
                                    }
                                }
                            }
                            else
                            {
                                string prev_pdf_file = path + ".pdf";

                                using (var document = PdfDocument.Open(prev_pdf_file))
                                {
                                    int page_count = document.NumberOfPages;
                                    var pages = document.GetPages();
                                    for (int page_index = 0; page_index < page_count; page_index++)
                                    //foreach (var page in document.GetPages())
                                    {
                                        var page = document.GetPage(page_index + 1);
                                        Console.Write($"page {page.Number} / {page_count}\n");
                                        current_block = -1;
                                        double prev_top = -10;
                                        List<List<Word>> block_to_words = new List<List<Word>>();
                                        foreach (var word in page.GetWords())
                                        {
                                            double top = page.Height - word.BoundingBox.Top;
                                            if (top - prev_top > 10)
                                            {
                                                prev_top = top;
                                                block_to_words.Add(new List<Word>());
                                                current_block = block_to_words.Count - 1;
                                            }
                                            block_to_words[current_block].Add(word);
                                        }

                                        for (int i = 0; i < block_to_words.Count; i++)
                                        {
                                            Console.Write($"block {i} / {block_to_words.Count}\n");
                                            List<Word> newList = block_to_words[i].OrderBy(p => p.BoundingBox.Left).ToList();
                                            for (int j = 0; j < newList.Count; j++)
                                            {
                                                save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n",
                                                    page.Number - 1,
                                                    i,
                                                    newList[j].BoundingBox.Left,
                                                    newList[j].BoundingBox.Right,
                                                    page.Height - newList[j].BoundingBox.Top,
                                                    page.Height - newList[j].BoundingBox.Bottom, newList[j].Text);
                                            }
                                        }
                                        Console.Write($"end page {page.Number}\n");
                                    }
                                    if (save_ocr_text_value != "")
                                        System.IO.File.WriteAllText(path + "_ocr.txt", save_ocr_text_value, Encoding.Default);
                                    Console.Write(">> OCR 파일 생성 완료 " + path + "_ocr.txt\n");
                                }
                                return;
                            }
                        }
                        else
                        {
                            //	2. ocr 수행
                            DirectoryInfo d_ocr1 = new DirectoryInfo(path_ocr);
                            if (d_ocr1.Exists == false)
                            {
                                d_ocr1.Create();
                            }

                            for (int i = 0; i < ocr_files.Count; i++)
                            {
                                string path_ocr_text = path_ocr + "/" + System.IO.Path.GetFileNameWithoutExtension(ocr_files[i].FullName) + ".txt";
                                FileInfo fi_ocr = new FileInfo(path_ocr_text);
                                Console.Write(">> OCR 수행 중 " + (i + 1).ToString() + " / " + ocr_files.Count.ToString() + " ");
                                string json = "";
                                if (fi_ocr.Exists)
                                {
                                    json = System.IO.File.ReadAllText(path_ocr_text);
                                }
                                else
                                {
                                    ocr_processing_page_count++;
                                    json = DoOCR(ocr_files[i].FullName, ocr_files[i].Name, ocr_files[i].Extension.ToLower());
                                    System.IO.File.WriteAllText(path_ocr_text, json);
                                }

                                JObject root = JObject.Parse(json);
                                if (root.Count > 0)
                                {
                                    JArray images = (JArray)root["images"];
                                    if (images != null && images.Count > 0)
                                    {
                                        JObject image = (JObject)images[0];
                                        if (image != null && image.Count > 0)
                                        {
                                            JValue inferResult = (JValue)image["inferResult"];
                                            if (inferResult != null && inferResult.ToString() == "SUCCESS")
                                            {
                                                sucesses_count++;
                                                Console.Write("SUCCESS\n");
                                                continue;
                                            }
                                        }
                                    }
                                }
                                error_count++;
                                Console.Write("ERROR\n");
                                error_string += ",";
                                error_string += ocr_files[i].Name;
                            }
                        }

                        HPdfDoc pdf = new HPdfDoc();
                        pdf.UseKREncodings();
                        pdf.UseKRFonts();
                        HPdfFont kr_font = pdf.GetFont("Dotum", "KSC-EUC-H");

                        /*configure pdf-document to be compressed. */
                        pdf.SetCompressionMode(HPdfDoc.HPDF_COMP_ALL);

                        List<string> del_files_list = new List<string>();

                        for (current_page = 0; current_page < image_files.Count; current_page++)
                        {
                            HPdfImage image = null;
                            if (image_files[current_page].Extension.ToLower() == ".jpg" || image_files[current_page].Extension.ToLower() == ".jpeg")
                            {
                                string new_file_path = current_page.ToString() + ".jpg";
                                File.Copy(image_files[current_page].FullName, new_file_path, true);
                                image = pdf.LoadJpegImageFromFile(new_file_path);
                                del_files_list.Add(new_file_path);
                            }
                            else if (image_files[current_page].Extension.ToLower() == ".png")
                            {
                                string new_file_path = current_page.ToString() + ".png";
                                File.Copy(image_files[current_page].FullName, new_file_path, true);
                                image = pdf.LoadPngImageFromFile(new_file_path);
                                del_files_list.Add(new_file_path);
                            }
                            if (image != null)
                            {
                                if (use_prev_ocr)
                                {
                                    Console.Write(">> PDF 생성 중 " + (current_page + 1).ToString() + " / " + image_files.Count.ToString() + "\n");
                                    uint image_width = (uint)GetValueWithRatio((int)image.GetWidth());
                                    uint image_heigth = (uint)GetValueWithRatio((int)image.GetHeight());
                                    if (prev_ocr_datas.ContainsKey(current_page))
                                    {
                                        HPdfPage page = pdf.AddPage();
                                        page.SetWidth(image_width);
                                        page.SetHeight(image_heigth);
                                        int page_height = (int)image_heigth;

                                        page.BeginText();
                                        page.MoveTextPos(0, page_height);

                                        foreach (KeyValuePair<int, List<PrevOcrData>> kvp in prev_ocr_datas[current_page])
                                        {
                                            current_block = kvp.Key;
                                            save_text_line = "";
                                            for (int pod = 0; pod < kvp.Value.Count; pod++)
                                            {
                                                page_TextOut(page, page_height, kr_font,
                                                    kvp.Value[pod].line,
                                                    kvp.Value[pod].left,
                                                    kvp.Value[pod].right,
                                                    kvp.Value[pod].top,
                                                    kvp.Value[pod].bottom);
                                                save_text_line += kvp.Value[pod].line;
                                                save_text_line += " ";
                                            }

                                            save_text_line = save_text_line.Replace('\n', ' ');
                                            save_text_value += save_text_line;
                                            save_text_value += "\npage : ";
                                            save_text_value += current_page.ToString();
                                            save_text_value += ", block : ";
                                            save_text_value += current_block.ToString();
                                            save_text_value += "\n";
                                        }
                                        page.EndText();

                                        page.DrawImage(image, 0, 0, image_width, image_heigth);
                                    }
                                    else
                                    {
                                        HPdfPage page = pdf.AddPage();
                                        page.SetWidth(image_width);
                                        page.SetHeight(image_heigth);
                                        page.DrawImage(image, 0, 0, image_width, image_heigth);
                                    }
                                }
                                else
                                {
                                    string path_ocr_text = path_ocr + "/" + System.IO.Path.GetFileNameWithoutExtension(image_files[current_page].FullName) + ".txt";
                                    FileInfo fi_ocr = new FileInfo(path_ocr_text);
                                    Console.Write(">> PDF 생성 중 " + (current_page + 1).ToString() + " / " + image_files.Count.ToString() + "\n");
                                    current_block = 0;
                                    bool is_save_end = true;
                                    uint image_width = (uint)GetValueWithRatio((int)image.GetWidth());
                                    uint image_heigth = (uint)GetValueWithRatio((int)image.GetHeight());
                                    if (fi_ocr.Exists)
                                    {
                                        string json = System.IO.File.ReadAllText(path_ocr_text);
                                        List<FieldData> test = GetFields(json);

                                        HPdfPage page = pdf.AddPage();
                                        page.SetWidth(image_width);
                                        page.SetHeight(image_heigth);
                                        int page_height = (int)image_heigth;

                                        page.BeginText();
                                        page.MoveTextPos(0, page_height);

                                        float line_left = 0;
                                        float line_right = 0;
                                        float line_top = 0;
                                        float line_bottom = 0;
                                        float line_center = 0;
                                        float line_height = 0;
                                        string line_string = "";
                                        for (int i = 0; i < test.Count; i++)
                                        {
                                            float center_x = 0, center_y = 0;
                                            for (int j = 0; j < test[i].points.Count; j++)
                                            {
                                                center_x += GetValueWithRatio(test[i].points[j].GetX());
                                                center_y += GetValueWithRatio(test[i].points[j].GetY());
                                            }

                                            center_x /= test[i].points.Count;
                                            center_y /= test[i].points.Count;
                                            float minx = center_x, miny = center_y, maxx = center_x, maxy = center_y;

                                            for (int j = 0; j < test[i].points.Count; j++)
                                            {
                                                float px = GetValueWithRatio(test[i].points[j].GetX());
                                                float py = GetValueWithRatio(test[i].points[j].GetY());
                                                if (px < center_x && (minx == center_x || minx < px))
                                                {
                                                    minx = px;
                                                }
                                                else if (px > center_x && (maxx == center_x || maxx > px))
                                                {
                                                    maxx = px;
                                                }
                                                if (py < center_y && (miny == center_y || miny < py))
                                                {
                                                    miny = py;
                                                }
                                                else if (py > center_y && (maxy == center_y || maxy > py))
                                                {
                                                    maxy = py;
                                                }
                                            }

                                            //  한 라인인지 판별하여 추가하는 부분
                                            if (line_string == "")
                                            {
                                                line_left = minx;
                                                line_right = maxx;
                                                line_top = miny;
                                                line_bottom = maxy;
                                                line_center = center_y;
                                                line_height = maxy - miny;
                                                line_string = test[i].text;
                                                //  라인이 끝나는 문자열이면 바로 출력
                                                if (test[i].line_break)
                                                {
                                                    save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n", current_page, current_block, line_left, line_right, line_top, line_bottom, line_string);

                                                    page_TextOut(page, page_height, kr_font, line_string, line_left, line_right, line_top, line_bottom);
                                                    line_left = 0;
                                                    line_right = 0;
                                                    line_top = 0;
                                                    line_bottom = 0;
                                                    line_center = 0;
                                                    line_height = 0;
                                                    line_string = "";
                                                }
                                            }
                                            else
                                            {
                                                bool is_in_line = false;
                                                float temp_line_height = maxy - miny;
                                                float temp_line_center = center_y;
                                                float height_offset = Math.Abs(line_height - temp_line_height);
                                                float height_range = Math.Abs(line_height * 0.2f);
                                                if (height_offset < height_range) // 문자열 높이와 라인 높이 차이가 20% 이내일때
                                                {
                                                    float blank_width = minx - line_right;
                                                    if (blank_width < temp_line_height)  //  문자열 사이의 거리가 문자 높이보다 작을 때
                                                    {
                                                        is_in_line = true;
                                                        line_right = maxx;
                                                        line_top = Math.Min(line_top, miny);
                                                        line_bottom = Math.Max(line_bottom, maxy);
                                                        line_center = (line_top + line_bottom) / 2.0f;
                                                        line_string += " ";
                                                        line_string += test[i].text;

                                                        //  라인이 끝나는 문자열이면 바로 출력
                                                        if (test[i].line_break)
                                                        {
                                                            save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n", current_page, current_block, line_left, line_right, line_top, line_bottom, line_string);

                                                            page_TextOut(page, page_height, kr_font, line_string, line_left, line_right, line_top, line_bottom);
                                                            line_left = 0;
                                                            line_right = 0;
                                                            line_top = 0;
                                                            line_bottom = 0;
                                                            line_center = 0;
                                                            line_height = 0;
                                                            line_string = "";
                                                        }
                                                    }
                                                }

                                                if (is_in_line == false) //  한 라인이 아닐 경우
                                                {
                                                    //  지금까지 라인 출력
                                                    save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n", current_page, current_block, line_left, line_right, line_top, line_bottom, line_string);

                                                    page_TextOut(page, page_height, kr_font, line_string, line_left, line_right, line_top, line_bottom);

                                                    //  새로운 라인 생성
                                                    line_left = minx;
                                                    line_right = maxx;
                                                    line_top = miny;
                                                    line_bottom = maxy;
                                                    line_center = center_y;
                                                    line_height = maxy - miny;
                                                    line_string = test[i].text;

                                                    //  라인이 끝나는 문자열이면 바로 출력
                                                    if (test[i].line_break)
                                                    {
                                                        Console.Write(i.ToString() + "\n");

                                                        save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n", current_page, current_block, line_left, line_right, line_top, line_bottom, line_string);

                                                        page_TextOut(page, page_height, kr_font, line_string, line_left, line_right, line_top, line_bottom);
                                                        line_left = 0;
                                                        line_right = 0;
                                                        line_top = 0;
                                                        line_bottom = 0;
                                                        line_center = 0;
                                                        line_height = 0;
                                                        line_string = "";
                                                    }
                                                }
                                            }

                                            //float actual_font_size = (maxy - miny);
                                            //float font_ratio = 0.75f;
                                            //float prev_font_ratio = 0.75f;
                                            //float actual_width = maxx - minx;
                                            //float real_width = 0;
                                            //float real_width1 = 0;
                                            //bool is_ok = false;
                                            //while (is_ok == false)
                                            //{
                                            //    page.SetFontAndSize(kr_font, actual_font_size * font_ratio);
                                            //    page.MeasureText(test[i].text, actual_width, false, ref real_width);
                                            //    real_width1 = page.TextWidth(test[i].text);
                                            //    if (real_width > actual_width)
                                            //    {
                                            //        //	써보니 큼.
                                            //        prev_font_ratio = font_ratio;
                                            //        font_ratio -= 0.1f;
                                            //    }
                                            //    else
                                            //    {
                                            //        is_ok = true;
                                            //    }
                                            //}
                                            //float x_offset = (actual_width - real_width) / 2.0f;
                                            //float y_offset = (actual_font_size * (1.0f - font_ratio)) / 2.0f;
                                            //page.TextOut(minx + x_offset, page_height - maxy + y_offset, test[i].text);

                                            //current_pos_x = minx;
                                            //current_pos_y = maxy;

                                            save_text_line += test[i].text;
                                            save_text_line += " ";

                                            save_origin_text_value += test[i].text;
                                            save_origin_text_value += " ";

                                            if (test[i].line_break)
                                            {
                                                save_text_line = save_text_line.Replace('\n', ' ');
                                                save_text_value += save_text_line;
                                                save_text_value += "\npage : ";
                                                save_text_value += current_page.ToString();
                                                save_text_value += ", block : ";
                                                save_text_value += current_block.ToString();
                                                save_text_value += "\n";
                                                current_block++;
                                                is_save_end = true;
                                                save_text_line = "";
                                                save_origin_text_value += "\n";
                                            }
                                            else
                                            {
                                                is_save_end = false;
                                            }
                                        }

                                        if (line_string != "")
                                        {
                                            save_ocr_text_value += string.Format("{0},{1},{2},{3},{4},{5},{6}\n", current_page, current_block, line_left, line_right, line_top, line_bottom, line_string);

                                            page_TextOut(page, page_height, kr_font, line_string, line_left, line_right, line_top, line_bottom);
                                        }

                                        if (is_save_end == false)
                                        {
                                            save_text_value += "\npage : ";
                                            save_text_value += current_page.ToString();
                                            save_text_value += ", block : ";
                                            save_text_value += current_block.ToString();
                                            save_text_value += "\n";
                                            current_block++;
                                            is_save_end = true;
                                            save_text_line = "";
                                        }
                                        page.EndText();

                                        page.DrawImage(image, 0, 0, image_width, image_heigth);
                                    }
                                    else
                                    {
                                        HPdfPage page = pdf.AddPage();
                                        page.SetWidth(image_width);
                                        page.SetHeight(image_heigth);
                                        page.DrawImage(image, 0, 0, image_width, image_heigth);
                                    }
                                }
                            }
                        }

                        if(File.Exists("output.pdf"))
                        {
                            File.Delete("output.pdf");
                        }
                        pdf.SaveToFile("output.pdf");
                        if (File.Exists(path + ".pdf"))
                        {
                            File.Delete(path + ".pdf");
                        }
                        File.Move("output.pdf", path + ".pdf");
                        //pdf.SaveToFile(path + ".pdf");
                        Console.Write(">> PDF 생성 완료 " + path + ".pdf\n");

                        if (save_text_value != "")
                        {
                            if (File.Exists(path + ".txt"))
                            {
                                File.Delete(path + ".txt");
                            }
                            System.IO.File.WriteAllText(path + ".txt", save_text_value, Encoding.Default);
                        }

                        if (save_origin_text_value != "")
                            System.IO.File.WriteAllText(path + "_origin.txt", save_origin_text_value, Encoding.Default);

                        if (save_ocr_text_value != "")
                            System.IO.File.WriteAllText(path + "_ocr.txt", save_ocr_text_value, Encoding.Default);

                        DirectoryInfo d_ocr = new DirectoryInfo(path_ocr);
                        if (d_ocr.Exists)
                        {
                            d_ocr.Delete(true);
                        }
                        pdf_save_count++;

                        if (error_count > 0)
                        {
                            error_list.Add(string.Format("{0} 성공[{1}],실패[{2}]{3}", d.Name, sucesses_count, error_count, error_string));
                        }

                        for(int i=0; i< del_files_list.Count; i++)
                        {
                            if(File.Exists(del_files_list[i]))
                                File.Delete(del_files_list[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }

            DirectoryInfo[] dirs = d.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                FolderToPDF_Progressing(dir.FullName);
            }
        }

        //static void FolderToPDF_OLD(string path)
        //{
        //    DirectoryInfo d = new DirectoryInfo(path);
        //    FileInfo[] files = d.GetFiles();
        //    if (files.Length > 0)
        //    {
        //        FileStream fos = new FileStream(path + ".pdf", FileMode.Create);
        //        BufferedStream bos = new BufferedStream(fos);

        //        PDF pdf = new PDF(bos);
        //        Font f4 = new Font(
        //                pdf,
        //                "AdobeMyungjoStd-Medium");

        //        foreach (FileInfo file in files)
        //        {
        //            if (image_format.Contains(file.Extension.ToLower()))
        //            {
        //                Console.Write(">> OCR Processing...\n");

        //                string json = DoOCR(file.FullName, file.Name, file.Extension.ToLower());
        //                List<FieldData> test = GetFields(json);

        //                String fileName = file.FullName;
        //                FileStream fis1 = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        //                int image_type = ImageType.JPG;
        //                if (file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg")
        //                {
        //                    image_type = ImageType.JPG;
        //                }
        //                else if (file.Extension.ToLower() == ".png")
        //                {
        //                    image_type = ImageType.PNG;
        //                }
        //                else if (file.Extension.ToLower() == ".bmp")
        //                {
        //                    image_type = ImageType.BMP;
        //                }
        //                Image image1 = new Image(pdf, fis1, image_type);

        //                float[] page_size = { image1.GetWidth(), image1.GetHeight() };
        //                Page page = new Page(pdf, page_size);
        //                TextLine text = new TextLine(f4);

        //                f4.SetSize(14);
        //                for (int i = 0; i < test.Count; i++)
        //                {
        //                    XFont font = new XFont("Arial", 40);
        //                    int minx = 0, miny = 0, maxx = 0, maxy = 0;
        //                    for (int j = 0; j < test[i].points.Count; j++)
        //                    {
        //                        if (j == 0)
        //                        {
        //                            minx = (int)test[i].points[j].GetX();
        //                            maxx = (int)test[i].points[j].GetX();
        //                            miny = (int)test[i].points[j].GetY();
        //                            maxy = (int)test[i].points[j].GetY();
        //                        }
        //                        else
        //                        {
        //                            if (minx > (int)test[i].points[j].GetX()) minx = (int)test[i].points[j].GetX();
        //                            if (maxx < (int)test[i].points[j].GetX()) maxx = (int)test[i].points[j].GetX();
        //                            if (miny > (int)test[i].points[j].GetY()) miny = (int)test[i].points[j].GetY();
        //                            if (maxy < (int)test[i].points[j].GetY()) maxy = (int)test[i].points[j].GetY();
        //                        }
        //                    }
        //                    text.SetFont(f4);
        //                    text.SetText(test[i].text);
        //                    text.SetPosition(minx, (miny + maxy) / 2);
        //                    text.DrawOn(page);
        //                }

        //                image1.SetPosition(0.0f, 0.0f);
        //                image1.DrawOn(page);

        //            }
        //        }

        //        pdf.Complete();
        //        bos.Close();

        //        //PdfDocument doc = new PdfDocument();
        //        //int page_count = 0;
        //        ////PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
        //        //foreach (FileInfo file in files)
        //        //{
        //        //	if (image_format.Contains(file.Extension.ToLower()))
        //        //	{
        //        //		string json = DoOCR(file.FullName, file.Name, file.Extension.ToLower());
        //        //		List<FieldData> test = GetFields(json);

        //        //		doc.Pages.Add(new PdfPage());
        //        //		XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[doc.Pages.Count - 1]);
        //        //		XImage img = XImage.FromFile(file.FullName);

        //        //		if (img.Size.Width > img.Size.Height)
        //        //		{
        //        //			doc.Pages[doc.Pages.Count - 1].Orientation = PdfSharp.PageOrientation.Landscape;
        //        //		}

        //        //		doc.Pages[doc.Pages.Count - 1].Width = XUnit.FromPoint(img.Size.Width);
        //        //		doc.Pages[doc.Pages.Count - 1].Height = XUnit.FromPoint(img.Size.Height);

        //        //		xgr.DrawImage(img, 0, 0);

        //        //		page_count++;

        //        //	}
        //        //}
        //        //if (page_count > 0)
        //        //	doc.Save(path + ".pdf");
        //        //doc.Close();
        //    }

        //    DirectoryInfo[] dirs = d.GetDirectories();
        //    foreach (DirectoryInfo dir in dirs)
        //    {
        //        FolderToPDF_OLD(dir.FullName);
        //    }
        //}

        static long UnixTimeNow()
        {
            var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

        static string DoOCR(string imagePath, string file_name, string file_ext)
        {
            var imageBinary = GetImageBinary(imagePath);

            string guid = Guid.NewGuid().ToString();
            guid = guid.Replace("-", string.Empty);
            file_ext = file_ext.Replace(".", string.Empty);

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uriBase);
            webRequest.Method = "POST";
            webRequest.Headers.Add("X-OCR-SECRET", accessKey);
            webRequest.Timeout = 30 * 1000; // 30초 
                                            // ContentType은 지정된 것이 있으면 그것을 사용해준다. 
            webRequest.ContentType = "application/json; charset=utf-8";
            // json을 string type으로 입력해준다. 
            string postData = "{\"version\": \"V2\"," +
                "\"requestId\": \"" + guid + "\"," +
                "\"timestamp\": " + UnixTimeNow().ToString() + "," +
                "\"lang\": \"ko\"," +
                "\"images\": [{ \"format\": \"" + file_ext + "\", \"data\": \"" + Convert.ToBase64String(imageBinary) + "\", \"name\": \"" + file_name + "\"}]}";
            // 보낼 데이터를 byteArray로 바꿔준다. 
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // 요청 Data를 쓰는 데 사용할 Stream 개체를 가져온다. 
            Stream dataStream = webRequest.GetRequestStream();
            // Data를 전송한다.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // dataStream 개체 닫기 
            dataStream.Close();

            string responseText = string.Empty;
            using (WebResponse resp = webRequest.GetResponse())
            {
                Stream respStream = resp.GetResponseStream();
                using (StreamReader sr = new StreamReader(respStream))
                {
                    responseText = sr.ReadToEnd();
                }
            }

            Thread.Sleep(1000); //  실행 후 1초 delay, 여러개 실행할 경우, 연속으로 기능 수행하지 않도록 해줌
            return responseText;
        }
        static byte[] GetImageBinary(string path)
        {
            return File.ReadAllBytes(path);
        }
    }
}
