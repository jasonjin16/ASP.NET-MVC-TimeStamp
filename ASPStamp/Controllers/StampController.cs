using InvoiceStamp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace InvoiceStamp.Controllers
{
    public class StampController : Controller
    {
        public Stamp stamp=new Stamp();
        // GET: Stamp
        public ActionResult Index()
        {
            ViewData["stamps"] = getStampList();
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            string[] img_info = new string[3];
            if (file.ContentLength > 0)
            {
                // initialze Temp Directory
                string tempDirPath = Server.MapPath("~/Uploads/Stamps/temp");
                if (!Directory.Exists(tempDirPath))
                {
                    Directory.CreateDirectory(tempDirPath);
                }                

                var fileName = Path.GetFileName(file.FileName);
                var fullPath = Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                file.SaveAs(fullPath);
                Bitmap img = new Bitmap(fullPath);
                // get background image file url
                img_info[0] = Globals.ResolveServerUrl("/Uploads/Stamps/temp/" + Path.GetFileName(fullPath), false);
                img_info[1] = Convert.ToString(img.Width);
                img_info[2] = Convert.ToString(img.Height);
                img.Dispose();
            }
            //return View();
            return new JsonResult { Data = img_info };
        }

        [HttpPost]
        public ActionResult Add(Stamp t_stamp)
        {
            stamp = t_stamp;  
            createStampScreenshotImage();            
            return new JsonResult { Data = getStampList() };
        }
        
        public List<string> getStampList()
        {
            // get Stamp list
            List<string> stamp_list=new List<string>();            
            if (!Directory.Exists(Server.MapPath("~/Uploads/Stamps/screenshot"))) return null;
            if (!Directory.Exists(Server.MapPath("~/Uploads/Stamps/xml"))) return null;

            string[] xml_files = Directory.GetFiles(Server.MapPath("~/Uploads/Stamps/xml"), "*.xml");            
            string[] screenshot_files = Directory.GetFiles(Server.MapPath("~/Uploads/Stamps/screenshot"), "*.*");            
            //screenshot_files[i] = Globals.ResolveServerUrl("/Uploads/Stamps/screenshot/" + Path.GetFileName(t_img_files[i]), false);
            if (xml_files.Length == 0 || screenshot_files.Length == 0) return stamp_list;
            if (xml_files.Length != screenshot_files.Length) return stamp_list;

            foreach(string xml in xml_files){
                string xml_data = "";
                if (System.IO.File.Exists(xml)){
                    XmlTextReader reader = new XmlTextReader(xml);
                    int node_index = 0;
                    while (reader.Read()){
                        if (reader.NodeType == XmlNodeType.Text){
                            if (node_index == 0){
                                xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/screenshot/" + reader.Value, false);                                
                            }if (node_index == 1){
                                if (!reader.Value.Equals("none"))
                                {
                                    xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/temp/" + reader.Value, false);
                                }else
                                {
                                    xml_data = xml_data + "|none";
                                }
                            }else if(node_index>=2){
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
        
        public Color getColorFromRGBString(string rgb_str)
        {
            string[] real_str = rgb_str.Split('(')[1].Split(')')[0].Split(',');
            return Color.FromArgb(Convert.ToInt32(real_str[0]), Convert.ToInt32(real_str[1]), Convert.ToInt32(real_str[2]));
        }

        public void createStampScreenshotImage()
        {
            string img_path, fileName;
            Image bmp;
            Graphics g;
            SolidBrush brush;
            if (stamp.bg_url.Equals("none")){
                bmp = new Bitmap((int)stamp.width, (int)stamp.height);
                g = Graphics.FromImage(bmp);
                brush = new SolidBrush(getColorFromRGBString(stamp.bg_color));
                g.FillRectangle(brush, 0, 0, stamp.width, stamp.height);
                fileName = Globals.getCurrentMiliSecond() + ".JPG";
                img_path = Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName);
                if (System.IO.File.Exists(img_path)) System.IO.File.Delete(img_path);
                bmp.Save(img_path);
                bmp.Dispose();
                g.Dispose();
            }else{
                fileName = stamp.bg_url.Split('/')[stamp.bg_url.Split('/').Length - 1];
                img_path = Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName);
            }

            bmp = Image.FromFile(img_path);
            g = Graphics.FromImage(bmp);
            float ar = bmp.Width / stamp.width;

            if (stamp.title_fontSize > 0){                
                float rx = (stamp.title_posX) * ar;
                float ry = (stamp.title_posY) * ar;
                float rw = (stamp.title_width) * ar;
                float rh = (stamp.title_height) * ar;
                RectangleF rectf = new RectangleF(rx, ry, bmp.Width, bmp.Height);                
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                brush = new SolidBrush(Color.FromArgb(255, getColorFromRGBString(stamp.fg_color).R, getColorFromRGBString(stamp.fg_color).G, getColorFromRGBString(stamp.fg_color).B));
                g.DrawString(stamp.title.Replace("<br>", "\n"), new Font(stamp.title_fontName, stamp.title_fontSize), brush, rectf);
            }
            if (stamp.description_fontSize > 0){
                float rx = (stamp.description_posX) * ar;
                float ry = (stamp.description_posY) * ar;
                float rw = (stamp.description_width) * ar;
                float rh = (stamp.description_height) * ar;
                RectangleF rectf = new RectangleF(rx, ry, bmp.Width, bmp.Height);                
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                brush = new SolidBrush(Color.FromArgb(255, getColorFromRGBString(stamp.fg_color).R, getColorFromRGBString(stamp.fg_color).G, getColorFromRGBString(stamp.fg_color).B));
                g.DrawString(stamp.description.Replace("<br>", "\n"), new Font(stamp.description_fontName, stamp.description_fontSize), brush, rectf);
            }
            
            brush = new SolidBrush(getColorFromRGBString(stamp.border_color));
            Pen pen = new Pen(brush, 4);
            g.DrawRectangle(pen, 0, 0, stamp.width*ar, stamp.height*ar);
            g.Flush();

            string tempDirPath = Server.MapPath("~/Uploads/Stamps/screenshot");
            if (!Directory.Exists(tempDirPath)) Directory.CreateDirectory(tempDirPath);
            if (System.IO.File.Exists(Path.Combine(tempDirPath, fileName)))
                System.IO.File.Delete(Path.Combine(tempDirPath, fileName));
            bmp.Save(Path.Combine(tempDirPath, fileName));
            bmp.Dispose();
            g.Dispose();

            createStampXMLFile(fileName);
        }

        public void createStampXMLFile(string fileName)
        {
            string tempDirPath = Server.MapPath("~/Uploads/Stamps/xml");
            if (!Directory.Exists(tempDirPath)) Directory.CreateDirectory(tempDirPath);
            XmlTextWriter writer = new XmlTextWriter(Path.Combine(tempDirPath, fileName.Split('.')[0]+ ".xml"), System.Text.Encoding.UTF8);
            writer.WriteStartDocument(true);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            writer.WriteStartElement("Stamp");
            createNode("StampImageURL", fileName, writer);
            if (stamp.bg_url.Equals("none"))
            {
                createNode("BackgroundImageURL", stamp.bg_url, writer);
            }
            else
            {
                createNode("BackgroundImageURL", stamp.bg_url.Split('/')[stamp.bg_url.Split('/').Length-1], writer);
            }
            createNode("Width", Convert.ToString(stamp.width), writer);
            createNode("Height", Convert.ToString(stamp.height), writer);  
            //--- title ---          
            createNode("Title", stamp.title, writer);
            createNode("TitleFontName", stamp.title_fontName, writer);
            createNode("TitleFontSize", Convert.ToString(stamp.title_fontSize), writer);
            createNode("TitleX", Convert.ToString(stamp.title_posX), writer);
            createNode("TitleY", Convert.ToString(stamp.title_posY), writer);
            createNode("TitleWidth", Convert.ToString(stamp.title_width), writer);
            createNode("TitleHeight", Convert.ToString(stamp.title_height), writer);
            //--- description ---
            createNode("Description", stamp.description, writer);            
            createNode("DescriptionFontName", stamp.description_fontName, writer);            
            createNode("DescriptionFontSize", Convert.ToString(stamp.description_fontSize), writer);            
            createNode("DescriptionX", Convert.ToString(stamp.description_posX), writer);
            createNode("DescriptionY", Convert.ToString(stamp.description_posY), writer);
            createNode("DescriptionWidth", Convert.ToString(stamp.description_width), writer);
            createNode("DescriptionHeight", Convert.ToString(stamp.description_height), writer);

            if (stamp.bg_url.Equals("none")){                
                createNode("BackgroundColor", stamp.bg_color, writer);
            }else{
                createNode("BackgroundColor", "none", writer);
            }            
            createNode("ForegroundColor", stamp.fg_color, writer);
            createNode("BorderColor", stamp.border_color, writer);
            //getColorFromRGBString(stamp.border_color).R.ToString("X2") + getColorFromRGBString(stamp.border_color).G.ToString("X2") + getColorFromRGBString(stamp.border_color).B.ToString("X2")    
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();            
        }

        private void createNode(string tagName, string tagVal, XmlTextWriter writer)
        {
            writer.WriteStartElement(tagName);
            writer.WriteString(tagVal);
            writer.WriteEndElement();
        }
    
        [HttpPost]
        public ActionResult Edit(Stamp sel_stamp)
        {
            string fileName = sel_stamp.stamp_url.Split('/')[sel_stamp.stamp_url.Split('/').Length-1];
            string fullPath = Path.Combine(Server.MapPath("~/Uploads/Stamps/xml"), fileName.Split('.')[0]+".xml");            
            string xml_data = "";
            if (System.IO.File.Exists(fullPath)) {
                XmlTextReader reader = new XmlTextReader(fullPath);
                int node_index=0;
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Text) {
                        if (node_index == 0)
                        {
                            xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/screenshot/" + reader.Value, false);
                        }
                        if (node_index == 1)
                        {
                            if (reader.Value.Equals("none")){
                                xml_data = xml_data + "|none";
                            }else{
                                xml_data = xml_data + "|" + Globals.ResolveServerUrl("/Uploads/Stamps/temp/" + reader.Value, false);
                            }                            
                        }
                        else if (node_index >= 2)
                        {
                            xml_data = xml_data + "|" + reader.Value;
                        }
                        ++node_index;
                    } 
                }
                reader.Dispose();
            }
            return new JsonResult { Data = xml_data };
        }

        [HttpPost]
        public ActionResult Delete(Stamp sel_stamp)
        {
            deleteAction(sel_stamp);
            return new JsonResult { Data = getStampList() };
        }

        [HttpPost]
        public ActionResult Update(Stamp sel_stamp)
        {
            string militime="", fileName="";
            if (!sel_stamp.bg_url.Equals("none"))
            {
                militime = Globals.getCurrentMiliSecond();
                fileName = sel_stamp.bg_url.Split('/')[sel_stamp.bg_url.Split('/').Length-1];
                if (System.IO.File.Exists(Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), militime + fileName)))
                    System.IO.File.Delete(Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), militime + fileName));
                System.IO.File.Copy(Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName), Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), militime + fileName));
            }
            deleteAction(sel_stamp);
            if (militime.Length >= 0 && fileName.Length > 0)
            {
                System.IO.File.Copy(Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), militime + fileName), Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName));
                System.IO.File.Delete(Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), militime + fileName));
            }
            stamp = sel_stamp;
            createStampScreenshotImage();
            return new JsonResult { Data = getStampList() };
        }

        public void deleteAction(Stamp sel_stamp)
        {
            string fileName = sel_stamp.stamp_url.Split('/')[sel_stamp.stamp_url.Split('/').Length - 1];
            string fullPath = Path.Combine(Server.MapPath("~/Uploads/Stamps/xml"), fileName.Split('.')[0] + ".xml");
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            fullPath = Path.Combine(Server.MapPath("~/Uploads/Stamps/temp"), fileName);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            fullPath = Path.Combine(Server.MapPath("~/Uploads/Stamps/screenshot"), fileName);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}