using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using BOOKLY.Application.Common;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace BOOKLY.Infrastructure.Email
{
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _emailOptions;
        private readonly FrontendOptions _frontendOptions;

        public SmtpEmailService(
            IOptions<EmailOptions> emailOptions,
            IOptions<FrontendOptions> frontendOptions)
        {
            _emailOptions = emailOptions.Value;
            _frontendOptions = frontendOptions.Value;
        }

        public Task SendEmailConfirmation(EmailConfirmationEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Confirma tu cuenta en BOOKLY",
                BooklyEmailTemplates.BuildEmailConfirmationBody(
                    model.RecipientName,
                    BuildUrl(_frontendOptions.ConfirmEmailPath, "Frontend:ConfirmEmailPath", model.Token),
                    model.ExpirationHours),
                ct);

        public Task SendPasswordReset(PasswordResetEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Restablece tu contrasena en BOOKLY",
                BooklyEmailTemplates.BuildPasswordResetBody(
                    model.RecipientName,
                    BuildUrl(_frontendOptions.ResetPasswordPath, "Frontend:ResetPasswordPath", model.Token),
                    model.ExpirationHours),
                ct);

        public Task SendSecretaryInvitation(SecretaryInvitationEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Invitacion para comenzar en BOOKLY",
                BooklyEmailTemplates.BuildSecretaryInvitationBody(
                    model.RecipientName,
                    model.InvitedByName,
                    model.ServiceName,
                    BuildUrl(
                        _frontendOptions.CompleteSecretaryInvitationPath,
                        "Frontend:CompleteSecretaryInvitationPath",
                        model.Token),
                    model.ExpirationHours),
                ct);

        public Task SendAdminInvitation(AdminInvitationEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Invitacion para administrar BOOKLY",
                BooklyEmailTemplates.BuildAdminInvitationBody(
                    model.RecipientName,
                    BuildUrl(
                        _frontendOptions.CompleteAdminInvitationPath,
                        "Frontend:CompleteAdminInvitationPath",
                        model.Token),
                    model.ExpirationHours),
                ct);

        public Task SendAppointmentCreatedToClient(AppointmentCreatedClientEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Confirmacion de turno",
                BooklyEmailTemplates.BuildAppointmentCreatedClientBody(
                    model.RecipientName,
                    model.ServiceName,
                    model.BusinessName,
                    model.StartDateTime,
                    model.DurationMinutes),
                ct);

        public Task SendAppointmentCreatedToOwner(AppointmentCreatedOwnerEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Nuevo turno agendado",
                BooklyEmailTemplates.BuildAppointmentCreatedOwnerBody(
                    model.RecipientName,
                    model.ClientName,
                    model.ClientEmail,
                    model.ClientPhone,
                    model.ServiceName,
                    model.StartDateTime,
                    model.DurationMinutes),
                ct);

        public Task SendAppointmentCancelledToClient(AppointmentCancelledClientEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Tu turno fue cancelado",
                BooklyEmailTemplates.BuildAppointmentCancelledClientBody(
                    model.RecipientName,
                    model.ServiceName,
                    model.BusinessName,
                    model.StartDateTime,
                    model.CancelReason),
                ct);

        public Task SendAppointmentCancelledToOwner(AppointmentCancelledOwnerEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Se cancelo un turno",
                BooklyEmailTemplates.BuildAppointmentCancelledOwnerBody(
                    model.RecipientName,
                    model.ClientName,
                    model.ClientEmail,
                    model.ClientPhone,
                    model.ServiceName,
                    model.StartDateTime,
                    model.CancelReason),
                ct);

        public Task SendAppointmentRescheduledToClient(AppointmentRescheduledClientEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Tu turno fue reprogramado",
                BooklyEmailTemplates.BuildAppointmentRescheduledClientBody(
                    model.RecipientName,
                    model.ServiceName,
                    model.BusinessName,
                    model.PreviousStartDateTime,
                    model.NewStartDateTime,
                    model.DurationMinutes),
                ct);

        public Task SendAppointmentRescheduledToOwner(AppointmentRescheduledOwnerEmailModel model, CancellationToken ct = default)
            => SendAsync(
                model.RecipientEmail,
                model.RecipientName,
                "Se reprogramo un turno",
                BooklyEmailTemplates.BuildAppointmentRescheduledOwnerBody(
                    model.RecipientName,
                    model.ClientName,
                    model.ClientEmail,
                    model.ClientPhone,
                    model.ServiceName,
                    model.PreviousStartDateTime,
                    model.NewStartDateTime,
                    model.DurationMinutes),
                ct);

        private async Task SendAsync(
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

            using var message = new MailMessage
            {
                From = new MailAddress(_emailOptions.SenderAddress, _emailOptions.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(recipientEmail, recipientName));

            using var client = new SmtpClient(_emailOptions.Smtp.Host, _emailOptions.Smtp.Port)
            {
                EnableSsl = _emailOptions.Smtp.EnableSsl
            };

            if (!string.IsNullOrWhiteSpace(_emailOptions.Smtp.Username) ||
                !string.IsNullOrWhiteSpace(_emailOptions.Smtp.Password))
            {
                client.Credentials = new NetworkCredential(_emailOptions.Smtp.Username, _emailOptions.Smtp.Password);
            }

            ct.ThrowIfCancellationRequested();
            await client.SendMailAsync(message);
        }

        private List<string> GetMissingEmailSettings()
        {
            var missingSettings = new List<string>();

            if (string.IsNullOrWhiteSpace(_emailOptions.SenderAddress))
                missingSettings.Add("Email:SenderAddress");

            if (string.IsNullOrWhiteSpace(_emailOptions.Smtp.Host))
                missingSettings.Add("Email:Smtp:Host");

            if (_emailOptions.Smtp.Port <= 0)
                missingSettings.Add("Email:Smtp:Port");

            return missingSettings;
        }

        private string BuildUrl(string path, string pathSettingName, string token)
        {
            if (string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl))
                throw new InvalidOperationException("No se puede construir el link del email. Completa: Frontend:BaseUrl.");

            var normalizedBaseUrl = $"{_frontendOptions.BaseUrl.TrimEnd('/')}/";
            if (!Uri.TryCreate(normalizedBaseUrl, UriKind.Absolute, out var baseUri))
                throw new InvalidOperationException("Frontend:BaseUrl debe ser una URL absoluta valida.");

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException(
                    $"No se puede construir el link del email. Completa: {pathSettingName}.");
            }

            var targetUri = new Uri(baseUri, path.TrimStart('/'));
            var builder = new UriBuilder(targetUri);
            var tokenQuery = $"token={Uri.EscapeDataString(token)}";
            var existingQuery = builder.Query.TrimStart('?');

            builder.Query = string.IsNullOrWhiteSpace(existingQuery)
                ? tokenQuery
                : $"{existingQuery}&{tokenQuery}";

            return builder.Uri.ToString();
        }
    }
}
