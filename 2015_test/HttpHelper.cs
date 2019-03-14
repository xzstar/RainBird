using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class HolidayData
    {
        public string date;
        public int weekDay;
        public string yearTips;
        public int type;
        public string typeDes;
        public string chineseZodiac;
        public string solarTerms;
        public string avoid;
        public string lunarCalendar;
        public string suit;
        public int dayOfYear;
        public int weekOfYear;
        public string constellation;

    }
    class Holiday
    {
        public int code;
        public string message;
        public HolidayData data;
    }
    class HttpHelper
    {
        private static string API_SERVER = "http://www.mxnzp.com/api/holiday/single/";

        private static string sCurrentDate = string.Empty;
        private static bool sIsHoliday = false;
        public static bool isHoliday()
        {
            string result = string.Empty;
            string date = String.Format("{0:D4}{1:D2}{2:D2}", DateTime.Now.ToLocalTime().Year,
                DateTime.Now.ToLocalTime().Month, DateTime.Now.ToLocalTime().Day);

            if(date == sCurrentDate)
            {
                return sIsHoliday;
            }
            else
            {
                try
                {
                    //{"code":1,"msg":"数据返回成功","data":{"date":"2018-11-21","weekDay":3,"yearTips":"戊戌","type":0,"typeDes":"工作日","chineseZodiac":"狗","solarTerms":"立冬后","avoid":"嫁娶.安葬","lunarCalendar":"十月十四","suit":"破屋.坏垣.祭祀.余事勿取","dayOfYear":325,"weekOfYear":47,"constellation":"天蝎座"}}
                    result = HttpGet(API_SERVER, date);
                    Holiday holiday = JsonConvert.DeserializeObject<Holiday>(result);
                    if(holiday.code != 1)
                    {
                        result = string.Empty;
                    }
                    else
                    {
                        sIsHoliday = holiday.data.type != 0  ||
                            DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Saturday ||
                            DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Sunday;
                    }
                    
                }
                catch (Exception e)
                {
                    result = string.Empty;
                }
                sCurrentDate = date;
                if (result == string.Empty)
                    sIsHoliday = (DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Saturday ||
                            DateTime.Now.ToLocalTime().DayOfWeek == DayOfWeek.Sunday);
                
                return sIsHoliday;

            }
        }

        public static string HttpPostToWechat(string content)
        {
            string result = string.Empty;
            string param = string.Format("content={0}", content);
            result = HttpPost(API_SERVER, param);
            return result;
        }
        public static string HttpPost(string url, string param)
        {
            var result = string.Empty;
            ////注意提交的编码 这边是需要改变的 这边默认的是Default：系统当前编码
            byte[] postData = Encoding.UTF8.GetBytes(param);
            try
            {
                // 设置提交的相关参数 
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                Encoding myEncoding = Encoding.UTF8;
                request.Method = "POST";
                request.KeepAlive = false;
                request.AllowAutoRedirect = true;
                request.ContentType = "application/x-www-form-urlencoded";
                request.UserAgent = "Billow 1.0";
                request.ContentLength = postData.Length;

                // 提交请求数据 
                System.IO.Stream outputStream = request.GetRequestStream();
                outputStream.Write(postData, 0, postData.Length);
                outputStream.Close();

                HttpWebResponse response;
                Stream responseStream;
                StreamReader reader;
                string srcString;
                response = request.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();
                reader = new System.IO.StreamReader(responseStream, Encoding.GetEncoding("UTF-8"));
                srcString = reader.ReadToEnd();
                result = srcString;   //返回值赋值
                reader.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine("HttpPost error" + e.Message);
                Log.log("HttpPost error" + e.Message);
            }
            return result;
        }


        public static string HttpGet(string Url, string postDataStr)
        {
            var result = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url +  postDataStr);
                request.Method = "GET";
                request.ContentType = "text/html;charset=UTF-8";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                result = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("HttpGet error" + e.Message);
                Log.log("HttpGet error" + e.Message);
                result = string.Empty;
            }
            return result;
        }
    }
}
