using BOOKLY.Application.Services.UserAggregate.DTOs;

namespace BOOKLY.Application.Interfaces
{
    public interface ISafeEmailDispatcher
    {
        Task TrySend(Func<Task> sendEmail, string purpose, string recipientEmail);

        Task<EmailDispatchResultDto> TrySendCritical(
            Func<Task> sendEmail,
            string purpose,
            string recipientEmail,
            string successMessage,
            string failureMessage);
    }
}
