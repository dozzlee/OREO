using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;

namespace Coded.Events.WebApi.Services
{
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            await ConfigSendGridasync(message);
        }
        
        // Send Email 
        private async Task ConfigSendGridasync(IdentityMessage message)
        {
             try
              {
                MailMessage mailMsg = new MailMessage();
                
                mailMsg.To.Add(message.Destination);
                mailMsg.From = new MailAddress("codedteam5@gmail.com", "CodedTeam5");

                mailMsg.Subject = message.Subject;
                string text = message.Body;
                string html = message.Body;
                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));

                // Init SmtpClient and send
                SmtpClient smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new NetworkCredential(ConfigurationManager.AppSettings["emailService:Account"], ConfigurationManager.AppSettings["emailService:Password"]);
                smtpClient.Credentials = credentials;

                await smtpClient.SendMailAsync(mailMsg);
              } catch (Exception ex) {
                Console.WriteLine(ex.Message);
              }
        }
    }
}
