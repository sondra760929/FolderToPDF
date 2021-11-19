using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PdfSharp.Drawing.Layout;
using PDFjet.NET;
using Font = PDFjet.NET.Font;
using HPdf;
using System.Diagnostics;
using System.Net.Mail;

namespace FolderToPDF
{

	class FieldData
    {
		public List<Point> points = new List<Point>();
		public string text;
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
        static void Main(string[] args)
		{
            if (args.Length > 0)
            {
				System.IO.FileInfo fi = new System.IO.FileInfo("C:/Program Files/DIGIBOOK/OCRToPDF/naver.txt");
				if(fi.Exists)
				{
					string[] lines = System.IO.File.ReadAllLines("C:/Program Files/DIGIBOOK/OCRToPDF/naver.txt");
					if (lines.Length == 2)
					{
						accessKey = lines[0];
						uriBase = lines[1];
					}
				}

				Console.Write(">> OCR To PDF Utility from DIGIBOOK 2021/11/11<<\n\n");

				Console.Write("암호를 입력하세요. : ");
				string input = Console.ReadLine();

				if(input != "ceohwang")
                {
					Console.Write("Wrong Password.\n");
					return;
				}

				Console.Write("처리 방식을 선택하세요. [all / 1 / 1-15] : ");
				input = Console.ReadLine();

				if(input.ToLower().Contains("all"))
                {
					ocr_all = true;
                }
                else
                {
					ocr_all = false;
					string[] pages = input.Split(',');
					for(int i=0; i<pages.Length; i++)
                    {
						int index = pages[i].IndexOf('-');
						if(index > 0)
                        {
							int start = 0;
							int end = 0;
							if (int.TryParse(pages[i].Substring(0, index), out start))
							{
								if (int.TryParse(pages[i].Substring(index+1, pages[i].Length - index - 1), out end))
								{
									if(end > start)
                                    {
										for(int j=start; j<=end; j++)
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
							if(index > 0)
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
					FolderToPDF(args[0]);
					string mail_content = "";
					mail_content = "OCR 처리 페이지 : " + ocr_processing_page_count.ToString() + "\n\n" + "PDF 생성 파일 개수 : " + pdf_save_count.ToString();
					SendEMail(DateTime.Now.ToString("yyyy-MM-dd HHmmss") + " OCR to PDF 결과 리포트", mail_content);
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
			catch(Exception e)
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
			return false;
        }
		static List<FieldData> GetFields(string json)
        {
			List<FieldData> return_value = new List<FieldData>();

			JObject root = JObject.Parse(json);
			if(root.Count > 0)
            {
				JArray images = (JArray)root["images"];
				for(int i=0; i<images.Count; i++)
                {
					JObject image = (JObject)images[i];
					if(image.Count > 0)
                    {
						JArray fields = (JArray)image["fields"];
						for(int j=0; j<fields.Count; j++)
                        {
							JObject field = (JObject)fields[j];
							if(field.Count > 0)
                            {
								FieldData fd = new FieldData();
								JObject boundingPoly = (JObject)field["boundingPoly"];
								if(boundingPoly.Count > 0)
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

								return_value.Add(fd);
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

		static void FolderToPDF(string path)
		{
			DirectoryInfo d = new DirectoryInfo(path);
			FileInfo[] files = d.GetFiles();
			if (files.Length > 0)
			{
				try
				{
					HPdfDoc pdf = new HPdfDoc();
					pdf.UseKREncodings();
					pdf.UseKRFonts();
					HPdfFont kr_font = pdf.GetFont("Dotum", "KSC-EUC-H");

					/*configure pdf-document to be compressed. */
					pdf.SetCompressionMode(HPdfDoc.HPDF_COMP_ALL);

					int current_page = 0;
					foreach (FileInfo file in files)
					{
						if (image_format.Contains(file.Extension.ToLower()))
						{
							String fileName = file.FullName;
							HPdfImage image = null;
							if (file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg")
							{
								image = pdf.LoadJpegImageFromFile(fileName);
							}
							else if (file.Extension.ToLower() == ".png")
							{
								image = pdf.LoadPngImageFromFile(fileName);
							}
							//else if (file.Extension.ToLower() == ".bmp")
							//{
							//	image = pdf.LoadRawImageFromFile(fileName);
							//}
							if (image != null)
							{
								current_page++;
								if (ocr_all || ocr_pages.Contains(current_page))
								{
									ocr_processing_page_count++;
									Console.Write(">> OCR Processing...\n");

									string json = DoOCR(file.FullName, file.Name, file.Extension.ToLower());
									List<FieldData> test = GetFields(json);

									HPdfPage page = pdf.AddPage();
									page.SetWidth(image.GetWidth());
									page.SetHeight(image.GetHeight());
									int page_height = (int)image.GetHeight();

									page.BeginText();
									page.MoveTextPos(0, page_height);
									//float current_pos_x = 0;
									//float current_pos_y = 0;
									for (int i = 0; i < test.Count; i++)
									{
										XFont font = new XFont("Arial", 40);
										int minx = 0, miny = 0, maxx = 0, maxy = 0;
										for (int j = 0; j < test[i].points.Count; j++)
										{
											if (j == 0)
											{
												minx = (int)test[i].points[j].GetX();
												maxx = (int)test[i].points[j].GetX();
												miny = (int)test[i].points[j].GetY();
												maxy = (int)test[i].points[j].GetY();
											}
											else
											{
												if (minx > (int)test[i].points[j].GetX()) minx = (int)test[i].points[j].GetX();
												if (maxx < (int)test[i].points[j].GetX()) maxx = (int)test[i].points[j].GetX();
												if (miny > (int)test[i].points[j].GetY()) miny = (int)test[i].points[j].GetY();
												if (maxy < (int)test[i].points[j].GetY()) maxy = (int)test[i].points[j].GetY();
											}
										}
										page.SetFontAndSize(kr_font, (maxy - miny) * 0.8f);
										//page.MoveTextPos(minx - current_pos_x, - (maxy - current_pos_y));
										//page.ShowText(test[i].text);

										uint len = 0;
										page.TextRect(minx, page_height - miny, maxx, page_height - maxy, test[i].text, HPdfTextAlignment.HPDF_TALIGN_CENTER, ref len);
										//current_pos_x = minx;
										//current_pos_y = maxy;
									}
									page.EndText();

									page.DrawImage(image, 0, 0, image.GetWidth(), image.GetHeight());
								}
							}
						}
					}
					pdf.SaveToFile(path + ".pdf");
					pdf_save_count++;

				}
				catch (Exception e)
				{
					Console.Error.WriteLine(e.Message);
				}
			}

			DirectoryInfo[] dirs = d.GetDirectories();
			foreach (DirectoryInfo dir in dirs)
			{
				FolderToPDF(dir.FullName);
			}
		}

		static void FolderToPDF_OLD(string path)
		{
			DirectoryInfo d = new DirectoryInfo(path);
			FileInfo[] files = d.GetFiles();
			if (files.Length > 0)
			{
				FileStream fos = new FileStream(path + ".pdf", FileMode.Create);
				BufferedStream bos = new BufferedStream(fos);

				PDF pdf = new PDF(bos);
				Font f4 = new Font(
						pdf,
						"AdobeMyungjoStd-Medium");

				foreach (FileInfo file in files)
				{
					if (image_format.Contains(file.Extension.ToLower()))
					{
						Console.Write(">> OCR Processing...\n");

						string json = DoOCR(file.FullName, file.Name, file.Extension.ToLower());
						List<FieldData> test = GetFields(json);

						String fileName = file.FullName;
						FileStream fis1 = new FileStream(fileName, FileMode.Open, FileAccess.Read);
						int image_type = ImageType.JPG;
						if (file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg")
						{
							image_type = ImageType.JPG;
						}
						else if (file.Extension.ToLower() == ".png")
						{
							image_type = ImageType.PNG;
						}
						else if (file.Extension.ToLower() == ".bmp")
						{
							image_type = ImageType.BMP;
						}
						Image image1 = new Image(pdf, fis1, image_type);

						float[] page_size = { image1.GetWidth(), image1.GetHeight() };
						Page page = new Page(pdf, page_size);
						TextLine text = new TextLine(f4);

						f4.SetSize(14);
						for (int i = 0; i < test.Count; i++)
						{
							XFont font = new XFont("Arial", 40);
							int minx = 0, miny = 0, maxx = 0, maxy = 0;
							for (int j = 0; j < test[i].points.Count; j++)
							{
								if (j == 0)
								{
									minx = (int)test[i].points[j].GetX();
									maxx = (int)test[i].points[j].GetX();
									miny = (int)test[i].points[j].GetY();
									maxy = (int)test[i].points[j].GetY();
								}
								else
								{
									if (minx > (int)test[i].points[j].GetX()) minx = (int)test[i].points[j].GetX();
									if (maxx < (int)test[i].points[j].GetX()) maxx = (int)test[i].points[j].GetX();
									if (miny > (int)test[i].points[j].GetY()) miny = (int)test[i].points[j].GetY();
									if (maxy < (int)test[i].points[j].GetY()) maxy = (int)test[i].points[j].GetY();
								}
							}
							text.SetFont(f4);
							text.SetText(test[i].text);
							text.SetPosition(minx, (miny + maxy) / 2);
							text.DrawOn(page);
						}

						image1.SetPosition(0.0f, 0.0f);
						image1.DrawOn(page);

					}
				}

				pdf.Complete();
				bos.Close();

				//PdfDocument doc = new PdfDocument();
				//int page_count = 0;
				////PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
				//foreach (FileInfo file in files)
				//{
				//	if (image_format.Contains(file.Extension.ToLower()))
				//	{
				//		string json = DoOCR(file.FullName, file.Name, file.Extension.ToLower());
				//		List<FieldData> test = GetFields(json);

				//		doc.Pages.Add(new PdfPage());
				//		XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[doc.Pages.Count - 1]);
				//		XImage img = XImage.FromFile(file.FullName);

				//		if (img.Size.Width > img.Size.Height)
				//		{
				//			doc.Pages[doc.Pages.Count - 1].Orientation = PdfSharp.PageOrientation.Landscape;
				//		}

				//		doc.Pages[doc.Pages.Count - 1].Width = XUnit.FromPoint(img.Size.Width);
				//		doc.Pages[doc.Pages.Count - 1].Height = XUnit.FromPoint(img.Size.Height);

				//		xgr.DrawImage(img, 0, 0);

				//		page_count++;

				//	}
				//}
				//if (page_count > 0)
				//	doc.Save(path + ".pdf");
				//doc.Close();
			}

			DirectoryInfo[] dirs = d.GetDirectories();
			foreach (DirectoryInfo dir in dirs)
			{
				FolderToPDF_OLD(dir.FullName);
			}
		}

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

			return responseText;
		}
		static byte[] GetImageBinary(string path)
		{
			return File.ReadAllBytes(path);
		}
	}
}
