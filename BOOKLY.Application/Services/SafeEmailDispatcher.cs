using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.UserAggregate.DTOs;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services
{
    // Los emails son complementarios: un fallo de envío se registra pero nunca revierte la operación principal.
    public sealed class SafeEmailDispatcher : ISafeEmailDispatcher
    {
        private readonly ILogger<SafeEmailDispatcher> _logger;

        public SafeEmailDispatcher(ILogger<SafeEmailDispatcher> logger)
        {
            _logger = logger;
        }

        public async Task TrySend(Func<Task> sendEmail, string purpose, string recipientEmail)
        {
            try
            {
                await sendEmail();
            }
            catch (Exception ex)
            {
                LogDispatchFailure(ex, purpose, recipientEmail);
            }
        }

        public async Task<EmailDispatchResultDto> TrySendCritical(
            Func<Task> sendEmail,
            string purpose,
            string recipientEmail,
            string successMessage,
            string failureMessage)
        {
            try
            {
                await sendEmail();
                return new EmailDispatchResultDto(true, successMessage);
            }
            catch (Exception ex)
            {
                LogDispatchFailure(ex, purpose, recipientEmail);
                return new EmailDispatchResultDto(false, failureMessage);
            }
        }

        private void LogDispatchFailure(Exception ex, string purpose, string recipientEmail)
        {
            _logger.LogWarning(
                ex,
                "La operación principal se completó, pero ocurrió un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                purpose,
                recipientEmail);
        }
    }
}
