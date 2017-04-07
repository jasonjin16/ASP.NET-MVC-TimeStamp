using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InvoiceStamp.Models
{
    public class Globals
    {
        public static string ResolveServerUrl(string serverUrl, bool forceHttps)
        {
            if (serverUrl.IndexOf("://") > -1)
                return serverUrl;

            string newUrl = serverUrl;
            Uri originalUri = System.Web.HttpContext.Current.Request.Url;
            newUrl = (forceHttps ? "https" : originalUri.Scheme) +
                "://" + originalUri.Authority + newUrl;
            return newUrl;
        }

        public static string getCurrentMiliSecond()
        {
            return DateTime.Now.Year.ToString("D4") + DateTime.Now.Month.ToString("D2") + DateTime.Now.Day.ToString("D2") + DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2") + DateTime.Now.Millisecond.ToString("D2");
        }

        public static string getFilePathOnSever(string url)
        {
            string[] tokens = url.Split(':')[2].Split('/');
            string filePath = tokens[1];
            for (int i = 2; i < tokens.Length; ++i) filePath += '\\' + tokens[i];
            return filePath;
        }
    }
}