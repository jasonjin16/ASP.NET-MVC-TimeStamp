using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace InvoiceStamp.Models
{
    public class Stamp
    {
        public string stamp_url { get; set; }
        public string bg_url { get; set; }
        public float width { get; set; }
        public float height { get; set; }
        // title
        public string title { get; set; }
        public string title_fontName { get; set; }
        public float title_fontSize { get; set; }
        public float title_posX { get; set; }
        public float title_posY { get; set; }
        public float title_width { get; set; }
        public float title_height { get; set; }
        // description
        public string description { get; set; }        
        public string description_fontName { get; set; }        
        public float description_fontSize { get; set; }        
        public float description_posX { get; set; }
        public float description_posY { get; set; }        
        public float description_width { get; set; }
        public float description_height { get; set; }        
        // color
        public string bg_color { get; set; }
        public string fg_color { get; set; }
        public string border_color { get; set; }        
    }
}