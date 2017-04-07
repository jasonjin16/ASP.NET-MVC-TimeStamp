using InvoiceStamp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace InvoiceStamp.Controllers
{
    public class FormController : Controller
    {
        // GET: Form
        public ActionResult Index()
        {
            // initialze Temp Directory
            string tempDirPath = Server.MapPath("~/Uploads/Templates/thumbs");
            if (!Directory.Exists(tempDirPath))
            {
                Directory.CreateDirectory(tempDirPath);
            }
            ViewData["templates"]= getTemplateList("");
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            string templateName;
            
            if (file.ContentLength > 0)
            {
                // save file
                templateName = Path.GetFileName(file.FileName).Replace(" ", "");                
                var militime = Globals.getCurrentMiliSecond();
                var fileName = militime + ".PDF";
                var fullPath = Path.Combine(Server.MapPath("~/Uploads/Templates"), fileName);
                file.SaveAs(fullPath);
                if(System.IO.File.Exists(Path.Combine(Server.MapPath("~/Uploads/Templates"), templateName)))
                {
                    System.IO.File.Delete(Path.Combine(Server.MapPath("~/Uploads/Templates"), templateName));
                }
                file.SaveAs(Path.Combine(Server.MapPath("~/Uploads/Templates"), templateName));

                // initialze Temp Directory
                string tempDirPath = Server.MapPath("~/Uploads/Templates/thumbs");
                if (!Directory.Exists(tempDirPath)) Directory.CreateDirectory(tempDirPath);
                if (!Directory.Exists(Server.MapPath("~/Uploads/Templates/thumbs/") + militime))
                {
                    Directory.CreateDirectory(Server.MapPath("~/Uploads/Templates/thumbs/") + militime);
                }else
                {
                    string[] files=Directory.GetFiles(Server.MapPath("~/Uploads/Templates/thumbs/") + militime, "*.png");
                    foreach (string img_file in files) System.IO.File.Delete(img_file);
                }
                    
                string command = Server.MapPath("~/Libs/gs/gswin64c.exe");
                string temp_dir = Server.MapPath("~/Uploads/Templates/thumbs/") + militime + "/%d.png ";
                string parameter = "-sDEVICE=png16m -r480 -dDownScaleFactor=3 -o " + temp_dir + fullPath;
                Process proc = new Process();
                proc.StartInfo.FileName = command;
                proc.StartInfo.Arguments = parameter;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.Start();
                proc.WaitForExit();
                // preprocess 
                System.IO.File.Delete(fullPath);
                if (!Directory.Exists(Server.MapPath("~/Uploads/Templates/thumbs/") + templateName.Split('.')[0])){
                    Directory.CreateDirectory(Server.MapPath("~/Uploads/Templates/thumbs/") + templateName.Split('.')[0]);
                }else{
                    string[] files = Directory.GetFiles(Server.MapPath("~/Uploads/Templates/thumbs/") + templateName.Split('.')[0], "*.png");
                    foreach (string img_file in files) System.IO.File.Delete(img_file);
                }
                // copy
                string[] img_files = Directory.GetFiles(Server.MapPath("~/Uploads/Templates/thumbs/") + militime, "*.png");
                for(int i=0; i<img_files.Length; ++i)
                {
                    System.IO.File.Copy(Server.MapPath("~/Uploads/Templates/thumbs/") + militime+"/"+(i+1)+".png", Server.MapPath("~/Uploads/Templates/thumbs/") + templateName.Split('.')[0] + "/" + (i + 1) + ".png");
                }
                // delete all image in temp dir
                string[] t_files = Directory.GetFiles(Server.MapPath("~/Uploads/Templates/thumbs/") + militime, "*.png");
                foreach (string img_file in t_files) System.IO.File.Delete(img_file);
                Directory.Delete(Server.MapPath("~/Uploads/Templates/thumbs/") + militime);

                return new JsonResult { Data = getTemplateList(templateName) };
            }else{
                return View();
            }            
        }

        public List<string> getTemplateList(string templateName) {
            // get image file list  
            List<string> templateImgUrls = new List<string>();
            string thumbs_url = Server.MapPath("~/Uploads/Templates/thumbs/");
            string[] thumbs_dirs = Directory.GetDirectories(Server.MapPath("~/Uploads/Templates/thumbs"));
            string[] template_urls = Directory.GetFiles(Server.MapPath("~/Uploads/Templates"), "*.PDF");
            for (int i = 0; i< thumbs_dirs.Length; ++i)
            {
                string template_url, cur_flag = "0";
                template_url = Globals.ResolveServerUrl("/Uploads/Templates/" + Path.GetFileName(template_urls[i]), false);

                string[] files = Directory.GetFiles(thumbs_dirs[i], "*.png");
                string thumbs = "";
                foreach (string file in files)
                {
                    string aaa = "/Uploads/Templates/thumbs/" + file.Split('\\')[file.Split('\\').Length - 2] + "/" + file.Split('\\')[file.Split('\\').Length - 1];
                    thumbs = thumbs + "|" + Globals.ResolveServerUrl("/Uploads/Templates/thumbs/" + file.Split('\\')[file.Split('\\').Length - 2] + "/" + file.Split('\\')[file.Split('\\').Length - 1], false);
                }
                if (Path.GetFileName(template_urls[i]).Equals(templateName)) cur_flag = "1";
                templateImgUrls.Add(template_url + "," + thumbs + ","+ cur_flag);
            }
            return templateImgUrls;
        }

        [HttpPost]
        public ActionResult Delete(Form form)
        {
            string templateName =form.formName.Split('/')[form.formName.Split('/').Length - 2];
            if (Directory.Exists(Server.MapPath("~/Uploads/Templates/thumbs/"+ templateName)))
            {
                string[] files = Directory.GetFiles(Server.MapPath("~/Uploads/Templates/thumbs/")+ templateName, "*.png");
                foreach (string file in files) System.IO.File.Delete(file);
            }
            Directory.Delete(Server.MapPath("~/Uploads/Templates/thumbs/"+ templateName));
            if (System.IO.File.Exists(Path.Combine(Server.MapPath("~/Uploads/Templates"), templateName + ".PDF")))
            {
                System.IO.File.Delete(Path.Combine(Server.MapPath("~/Uploads/Templates"), templateName + ".PDF"));
            }
            return new JsonResult { Data = getTemplateList("") };
        }
    }
}