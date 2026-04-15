using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using BOOKLY.Application.Common;
using Microsoft.Extensions.Options;

namespace BOOKLY.Infrastructure.Email
{
    public sealed class SmtpEmailService : EmailServiceBase
    {
        public SmtpEmailService(
            IOptions<EmailOptions> emailOptions,
            IOptions<FrontendOptions> frontendOptions)
            : base(emailOptions, frontendOptions)
        {
        }

        protected override async Task SendAsync(
            string recipientEmail,
            string recipientName,
            string subject,
            string htmlBody,
            CancellationToken ct)
        {
            var missingEmailSettings = GetMissingEmailSettings();
            if (missingEmailSettings.Count > 0)
            {
                throw new InvalidOperationException(
                    $"El servicio de email no esta configurado. Completa: {string.Join(", ", missingEmailSettings)}.");
            }

            if (Uri.TryCreate(EmailOptions.Smtp.Host, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "Email:Smtp:Host debe ser solo el host SMTP, sin http://, https:// ni rutas.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(EmailOptions.SenderAddress, EmailOptions.SenderName),
                Subject = subject,
                Body = BuildPlainTextBody(htmlBody),
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(recipientEmail, recipientName));
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(
                    htmlBody,
                    Encoding.UTF8,
                    MediaTypeNames.Text.Html));

            using var client = new SmtpClient(EmailOptions.Smtp.Host, EmailOptions.Smtp.Port)
            {
                UseDefaultCredentials = false,
                EnableSsl = EmailOptions.Smtp.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(EmailOptions.Smtp.Username) ||
                !string.IsNullOrWhiteSpace(EmailOptions.Smtp.Password))
            {
                client.Credentials = new NetworkCredential(EmailOptions.Smtp.Username, EmailOptions.Smtp.Password);
            }

            ct.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
        }

        private List<string> GetMissingEmailSettings()
        {
            var missingSettings = new List<string>();

            if (string.IsNullOrWhiteSpace(EmailOptions.SenderAddress))
                missingSettings.Add("Email:SenderAddress");

            if (string.IsNullOrWhiteSpace(EmailOptions.Smtp.Host))
                missingSettings.Add("Email:Smtp:Host");

            if (EmailOptions.Smtp.Port <= 0)
                missingSettings.Add("Email:Smtp:Port");

            return missingSettings;
        }

        private static string BuildPlainTextBody(string htmlBody)
        {
            var bodyWithLineBreaks = Regex.Replace(
                htmlBody,
                @"<(br|/p|/div|/li|/h1|/h2|/h3)\s*/?>",
                "\n",
                RegexOptions.IgnoreCase);

            var bodyWithoutTags = Regex.Replace(bodyWithLineBreaks, "<[^>]+>", string.Empty);
            var decodedBody = WebUtility.HtmlDecode(bodyWithoutTags);
            var normalizedBody = Regex.Replace(decodedBody, @"\n{3,}", "\n\n");

            return normalizedBody.Trim();
        }
    }
}
