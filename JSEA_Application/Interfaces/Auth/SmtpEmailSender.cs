using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JSEA_Application.Interfaces.Auth
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var smtp = _config.GetSection("Smtp");

            var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
            {
                Credentials = new NetworkCredential(
                    smtp["Username"],
                    smtp["Password"]
                ),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(
                    smtp["FromEmail"]!,
                    smtp["FromName"]
                ),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mail.To.Add(to);

            await client.SendMailAsync(mail);
        }
    }
}
