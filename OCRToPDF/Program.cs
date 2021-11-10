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
		private static int pdf_file_count = 0;
		private static int pdf_page_count = 0;
		private static int jpg_file_count = 0;
		static string accessKey = "";//"bnBrbU5NRWJBakZYTnRJTWJoY1NpV1ZwenFEYllEaHM=";
		static string uriBase = "";//"https://43d0993faa7143c0add4cbdfa7fac53d.apigw.ntruss.com/custom/v1/12325/800a9d7344b09c6a7ea43f71842cf3473eb134405af99c384a5a5c8c204b39d2/general";

		static void Main(string[] args)
		{
            if (args.Length > 0)
            {
				System.IO.FileInfo fi = new System.IO.FileInfo("C:/Program Files (x86)/DIGIBOOK/OCRToPDF/naver.txt");
				if(!fi.Exists)
				{
					Console.Write("OCR 설정 파일이 없습니다.");
					return;
				}

				string[] lines = System.IO.File.ReadAllLines("C:/Program Files (x86)/DIGIBOOK/OCRToPDF/naver.txt");
				if(lines.Length == 2)
                {
					accessKey = lines[0];
					uriBase = lines[1];
				}

				Console.Write(">> Folder To PDF Utility from DIGIBOOK 2019/03/22<<\n\n");
                FolderToPDF(args[0]);
                //Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
				Console.Clear();
				CheckPDFandImages(args[0]);
                Console.Write("\n");
                if (pdf_page_count != jpg_file_count)
                {
                    Console.Write("PDF Page Count is not same as Image File Count. ReRun PDFToFiles.\n");
                    int code = Console.Read();
                }
            }
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
							Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
						}
						catch (Exception e)
						{
							//throw new Pdf2KTException("Error while opening PDF document.", e);
						}
					}
					else if (image_format.Contains(file.Extension.ToLower()))
					{
						jpg_file_count++;
						Console.SetCursorPosition(0, 1);
						Console.Write(String.Format("Check PDF and Image Count : PDF Files ({0}) , PDF Pages ({1}), Image Files ({2})", pdf_file_count, pdf_page_count, jpg_file_count));
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
						if(file.Extension.ToLower() == ".jpg" || file.Extension.ToLower() == ".jpeg")
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
				FolderToPDF(dir.FullName);
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
