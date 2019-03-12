using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleProxy
{
    class MailService
    {
        public static bool MailSend(string subject, string content)
        {
           MailMessage msg = new MailMessage();

            msg.To.Add("77875238@qq.com");
            msg.From = new MailAddress("77875238@qq.com", "Billow");

            msg.Subject = subject;
            //标题格式为UTF8  
            msg.SubjectEncoding = Encoding.UTF8;

            msg.Body = content;
            //内容格式为UTF8 
            msg.BodyEncoding = Encoding.UTF8;

            SmtpClient client = new SmtpClient();
            //SMTP服务器地址 
            client.Host = "smtp.qq.com";
            //SMTP端口，QQ邮箱填写587  
            client.Port = 587;
            //启用SSL加密  
            client.EnableSsl = true;

            client.Credentials = new NetworkCredential("77875238@qq.com", "rjbnvfmuetdfcaed");
            //发送邮件  
            try
            {
                client.Send(msg);
                Console.WriteLine(subject + " 邮件发送成功");
                return true;
            }
            catch (SmtpException e)
            {
                Console.WriteLine(subject + " 邮件发送失败" + e.Message);
                return false;//发送失败
            }
            finally
            {
                client.Dispose();
                msg.Dispose();
            }                
        }

        public static void Notify(string subject, string content)
        {

            Task.Run(() => { MailSend(subject,content); });
            
        }
    }
}
