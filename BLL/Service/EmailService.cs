using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using BLL.Configurations;
using BLL.Interfaces.IServices;
using BLL.Dtos.EmailDto;

namespace BLL.Service
{
    public class EmailService : IMailService
    {
        private readonly EmailSettings _settings;

        // Sử dụng IOptions để nhận cấu hình từ appsettings.json
        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_settings.Email));
            email.To.Add(MailboxAddress.Parse(request.ToEmail));
            email.Subject = request.Subject;

            var builder = new BodyBuilder { HtmlBody = request.Body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_settings.Email, _settings.Password);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
