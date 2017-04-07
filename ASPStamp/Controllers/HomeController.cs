using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using InvoiceStamp.Models;
using System.Xml;
using System.Drawing;

namespace InvoiceStamp.Controllers
{
    public class HomeController : Controller
    {
        public string invoiceName;
        // GET: Home
        public ActionResult Index()
        {
            cleanInvoiceFiles();
            return View();
        }

        public void cleanInvoiceFiles() {
            if (!Directory.Exists(Server.MapPath("~/Uploads/Invoices"))) return;
            string[] files = Directory.GetFiles(Server.MapPath("~/Uploads/Invoices"), "*.PDF");
            DateTime now = DateTime.Now;
            foreach (string file in files) {
                DateTime dt=System.IO.File.GetCreationTime(file);
                double aa = (now - dt).TotalDays;
                if ((now - dt).TotalDays >= 2) {
                    System.IO.File.Delete(file);
                    if(Directory.Exists(Server.MapPath("~/Uploads/Invoices/temp") + "\\" + file.Split('.')[0]))
                    {
                        string[] temp_files = Directory.GetFiles(Server.MapPath("~/Uploads/Invoices/temp") + "\\" + file.Split('.')[0], "*.PDF");
                        foreach (string temp_file in temp_files) System.IO.File.Delete(temp_file);
                    }                    
                }
            }
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            List<string> invoiceImgUrls= new List<string>(); 
            if (file.ContentLength > 0)
            {
                invoiceName = Path.GetFileName(file.FileName).Replace(" ", "");
                var fullPath = Path.Combine(Server.MapPath("~/Uploads/Invoices"), invoiceName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                file.SaveAs(fullPath);
                invoiceImgUrls=preprocessInvoicePDF(fullPath, invoiceName.Split('.')[0]);
                invoiceImgUrls.Add(invoiceName);
            }
            //return View();
            return new JsonResult { Data = invoiceImgUrls };
        }

        public List<string> preprocessInvoicePDF(string fullPath, string invoiceName)
        {
            // initialze Temp Directory
            string tempDirPath = Server.MapPath("~/Uploads/Invoices/temp");
            if (!Directory.Exists(tempDirPath)) Directory.CreateDirectory(tempDirPath);
            if (!Directory.Exists(tempDirPath + "\\" + invoiceName))
            {
                Directory.CreateDirectory(tempDirPath + "\\" + invoiceName);
            }
            else
            {
                string[] files;
                files = Directory.GetFiles(tempDirPath + "\\" + invoiceName, "*.*");
                foreach (string file in files) System.IO.File.Delete(file);
            }

            string command = Server.MapPath("~/Libs/gs/gswin64c.exe");
            string parameter = "-sDEVICE=png16m -r480 -dDownScaleFactor=3 -o " + tempDirPath + "/" + invoiceName + "/%d.png " + fullPath;
            Process proc = new Process();
            proc.StartInfo.FileName = command;
            proc.StartInfo.Arguments = parameter;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
            proc.WaitForExit();

            // get image file list  
            ViewBag.invoiceName = this.invoiceName;
            List<string> imageUrls=new List<string>();
            string[] urls =Directory.GetFiles(tempDirPath + "/" + invoiceName, "*.png");
            foreach (string url in urls)
                imageUrls.Add(Globals.ResolveServerUrl("/Uploads/Invoices/temp/"+ invoiceName+"/" + Path.GetFileName(url), false));                     
            return imageUrls;
        }

        [HttpPost]
        public ActionResult GetStampList()
        {
            return new JsonResult { Data = getStampList() };
        }

        public List<string> getStampList()
        {
            // get Stamp list
            List<string> stamp_list = new List<string>();
            if (!Directory.Exists(Server.MapPath("~/Uploads/Stamps/screenshot"))) return null;
            if (!Directory.Exists(Server.MapPath("~/Uploads/Stamps/xml"))) return null;

            string[] xml_files = Directory.GetFiles(Server.MapPath("~/Uploads/Stamps/xml"), "*.xml");
            string[] screenshot_files = Directory.GetFiles(Server.MapPath("~/Uploads/Stamps/screenshot"), "*.*");
            //screenshot_files[i] = Globals.ResolveServerUrl("/Uploads/Stamps/screenshot/" + Path.GetFileName(t_img_files[i]), false);
            if (xml_files.Length == 0 || screenshot_files.Length == 0) return stamp_list;
            if (xml_files.Length != screenshot_files.Length) return stamp_list;

            foreach (string xml in xml_files)
            {
                string xml_data = "";
                if (System.IO.File.Exists(xml))
                {
                    XmlTextReader reader = new XmlTextReader(xml);
                    int node_index = 0;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            if (node_index == 0)
                            {
                                xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/screenshot/" + reader.Value, false);
                            }
                            if (node_index == 1)
                            {
                                if (!reader.Value.Equals("none"))
                                {
                                    xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/temp/" + reader.Value, false);
                                }
                                else
                                {
                                    xml_data = xml_data + "|none";
                                }
                            }
                            else if (node_index >= 2)
                            {
                                xml_data = xml_data + "|" + reader.Value;
                            }
                            ++node_index;
                        }
                    }
                    stamp_list.Add(xml_data);
                    reader.Close();
                    reader.Dispose();
                }
            }

            return stamp_list;
        }

        [HttpPost]
        public ActionResult GetTemplateList()
        {
            return new JsonResult { Data = getTemplateList("") };
        }
        public List<string> getTemplateList(string templateName)
        {
            // get image file list  
            List<string> templateImgUrls = new List<string>();
            string thumbs_url = Server.MapPath("~/Uploads/Templates/thumbs/");
            string[] thumbs_dirs = Directory.GetDirectories(Server.MapPath("~/Uploads/Templates/thumbs"));
            string[] template_urls = Directory.GetFiles(Server.MapPath("~/Uploads/Templates"), "*.PDF");
            for (int i = 0; i < thumbs_dirs.Length; ++i)
            {
                string template_url, cur_flag = "0";
                template_url = Globals.ResolveServerUrl("/Uploads/Templates/" + Path.GetFileName(template_urls[i]), false);

                string[] files = Directory.GetFiles(thumbs_dirs[i], "*.png");
                string thumbs = "";
                foreach (string file in files)
                {
                    //string aaa = "/Uploads/Templates/thumbs/" + file.Split('\\')[file.Split('\\').Length - 2] + "/" + file.Split('\\')[file.Split('\\').Length - 1];
                    thumbs = thumbs + "|" + Globals.ResolveServerUrl("/Uploads/Templates/thumbs/" + file.Split('\\')[file.Split('\\').Length - 2] + "/" + file.Split('\\')[file.Split('\\').Length - 1], false);
                }
                if (Path.GetFileName(template_urls[i]).Equals(templateName)) cur_flag = "1";
                templateImgUrls.Add(template_url + "," + thumbs + "," + cur_flag);
            }
            return templateImgUrls;
        }

        [HttpPost]
        public ActionResult GetImageDimentsion(Home invoice)
        {            
            System.Drawing.Image invoice_img = System.Drawing.Image.FromFile(Server.MapPath("~/"+ Globals.getFilePathOnSever(invoice.InvoiceImageURL)));
            System.Drawing.Image stamp_img = System.Drawing.Image.FromFile(Server.MapPath("~/" + Globals.getFilePathOnSever(invoice.StampImageURL)));
            string response = invoice_img.Width + "," + invoice_img.Height + "|" + stamp_img.Width + "," + stamp_img.Height;
            invoice_img.Dispose();
            stamp_img.Dispose();
            return new JsonResult { Data= response };
        }
        
        [HttpPost]
        public ActionResult SaveInvoice(Home invoice)
        {            
            if (invoice.SaveData.Split('|').Length <= 0) return new JsonResult { Data = "none" };
            float ar = float.Parse(invoice.SaveData.Split(';')[1]);
            FileStream fs;
            Document doc;
            PdfWriter writer;
            string invoice_fileName = Globals.getCurrentMiliSecond() + ".PDF";
            string[] pages = invoice.SaveData.Split(';')[0].Split('|');
            // create temp pdf file
            fs = new FileStream(Path.Combine(Server.MapPath("~/Uploads/Generated/") + "temp_" + invoice_fileName), FileMode.Create, FileAccess.Write, FileShare.None);
            doc = new Document(PageSize.A4);
            writer = PdfWriter.GetInstance(doc, fs);
            doc.Open();
            for (int i = 1; i < pages.Length; ++i) { doc.NewPage();  doc.Add(new Paragraph(" ")); }
            doc.Close();
            fs.Close();
            // open with stream
            FileStream readerStream, writerStream;
            readerStream = new FileStream(Path.Combine(Server.MapPath("~/Uploads/Generated/") + "temp_" + invoice_fileName), FileMode.Open);
            writerStream = new FileStream(Path.Combine(Server.MapPath("~/Uploads/Generated/") + invoice_fileName), FileMode.Create, FileAccess.Write);

            using (Stream oldStream = readerStream)
            using (Stream newStream = writerStream)
            {
                PdfReader pdfReader = new PdfReader(oldStream);
                PdfStamper pdfStamper = new PdfStamper(pdfReader, newStream);
                //BaseFont baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1250, BaseFont.NOT_EMBEDDED);                

                for (int i = 1; i <= pdfReader.NumberOfPages; ++i)
                {
                    PdfContentByte pdfContentByte = pdfStamper.GetOverContent(i);
                    PdfGState pdfGState = new PdfGState();
                    pdfContentByte.SetGState(pdfGState);

                    string page = pages[i];
                    string page_imgName = Server.MapPath("~/") + Globals.getFilePathOnSever(page.Split('=')[0]);
                    iTextSharp.text.Image page_img = iTextSharp.text.Image.GetInstance(page_imgName);
                    page_img.ScaleToFit(PageSize.A4.Width, PageSize.A4.Height);
                    page_img.SetAbsolutePosition(0, 0);
                    pdfContentByte.AddImage(page_img);

                    ar =  PageSize.A4.Width/ page_img.Width/ar;
                    string[] stamps = page.Split('=')[1].Split('_');
                    for (int j = 1; j < stamps.Length; ++j)
                    {
                        string[] stamp = stamps[j].Split(',');
                        string stamp_imgName = Server.MapPath("~/") + Globals.getFilePathOnSever(stamp[0]);
                        iTextSharp.text.Image stamp_img = iTextSharp.text.Image.GetInstance(stamp_imgName);                        
                        
                        float stamp_x = float.Parse(stamp[1].Substring(0, stamp[1].Length - 2));
                        float stamp_y = float.Parse(stamp[2].Substring(0, stamp[2].Length - 2));
                        float stamp_wid = float.Parse(stamp[3]);
                        float stamp_hei = float.Parse(stamp[4]);
                        stamp_img.ScaleToFit(stamp_wid * ar, stamp_hei * ar);
                        stamp_img.SetAbsolutePosition(stamp_x * ar, PageSize.A4.Height + stamp_y * ar);
                        pdfContentByte.AddImage(stamp_img);
                    }
                }
                pdfStamper.Close();                
            }
            readerStream.Dispose();
            writerStream.Dispose();

            System.IO.File.Delete(Path.Combine(Server.MapPath("~/Uploads/Generated/") + "temp_" + invoice_fileName));

            return new JsonResult { Data = Globals.ResolveServerUrl("/Uploads/Generated/" + invoice_fileName, false) };
        }
    }
}