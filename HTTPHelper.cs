using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Wemew.Program.Models;
using Wemew.Program.Models.Protocol;

namespace Wemew.Program.Assets.utility
{
    public static class HTTPHelper
    {
        public static string GET(string URL,string parameter)
        {
            using (var webClient = new WebClient())
            {
                //发起网络请求
                webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                var paraURL = $"{URL}{parameter}";
                var resBytes = webClient.DownloadData(paraURL);
                var resString = Encoding.UTF8.GetString(resBytes);
                return resString;
            }
        }
        public static MemoryStream GETQRCode(string URL, string parameter)
        {
            using (var webClient = new WebClient())
            {
                //发起网络请求
                webClient.Headers.Add("Content-Type", "image/jpeg;charset=UTF-8");
                var paraURL = $"{URL}{parameter}";
                var resBytes = webClient.DownloadData(paraURL);
                MemoryStream ms = new MemoryStream(resBytes);
                return ms;
            }
        }

        public static string POST(string URL, string parameter)
        {
            using (var webClient = new WebClient())
            {
                //发起网络请求
                webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                var paramString = parameter;
                var paramBytes = Encoding.UTF8.GetBytes(paramString);
                var resBytes = webClient.UploadData(URL, "POST", paramBytes);
                var resString = Encoding.UTF8.GetString(resBytes);
                return resString;
            }
        }
    }
}
