//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace ConsoleProxy
//{
//    class HttpHelper
//    {
//        private static string WECHAT_SERVER = "http://localhost:10245/message";
//        private static string WECHAT_SERVER_KEY = "content";
//        public static string HttpPostToWechat(string content)
//        {
//            string result = string.Empty;
//            string param = string.Format("content={0}", content);
//            result = HttpPost(WECHAT_SERVER, param);
//            return result;
//        }
//        public static string HttpPost(string url, string param)
//        {
//            var result = string.Empty;
//            ////注意提交的编码 这边是需要改变的 这边默认的是Default：系统当前编码
//            byte[] postData = Encoding.UTF8.GetBytes(param);
//            try {
//                // 设置提交的相关参数 
//                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
//                Encoding myEncoding = Encoding.UTF8;
//                request.Method = "POST";
//                request.KeepAlive = false;
//                request.AllowAutoRedirect = true;
//                request.ContentType = "application/x-www-form-urlencoded";
//                request.UserAgent = "Billow 1.0";
//                request.ContentLength = postData.Length;

//                // 提交请求数据 
//                System.IO.Stream outputStream = request.GetRequestStream();
//                outputStream.Write(postData, 0, postData.Length);
//                outputStream.Close();

//                HttpWebResponse response;
//                Stream responseStream;
//                StreamReader reader;
//                string srcString;
//                response = request.GetResponse() as HttpWebResponse;
//                responseStream = response.GetResponseStream();
//                reader = new System.IO.StreamReader(responseStream, Encoding.GetEncoding("UTF-8"));
//                srcString = reader.ReadToEnd();
//                result = srcString;   //返回值赋值
//                reader.Close();

//            }
//            catch (Exception e)
//            {
//                Console.WriteLine("HttpPost error" + e.Message);
//                Log.log("HttpPost error" + e.Message);
//            }
//            return result;
//        }
        

//        public static string HttpGet(string Url, string postDataStr)
//        {
//            var result = string.Empty;
//            try
//            {
//                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
//                request.Method = "GET";
//                request.ContentType = "text/html;charset=UTF-8";

//                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
//                Stream myResponseStream = response.GetResponseStream();
//                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
//                result = myStreamReader.ReadToEnd();
//                myStreamReader.Close();
//                myResponseStream.Close();
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine("HttpPost error" + e.Message);
//                Log.log("HttpPost error" + e.Message);
//            }
//            return result;
//        }
//    }
//}
