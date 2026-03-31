using BOOKLY.Domain.Emailing;

namespace BOOKLY.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailConfirmation(EmailConfirmationEmailModel model, CancellationToken ct = default);
        Task SendPasswordReset(PasswordResetEmailModel model, CancellationToken ct = default);
        Task SendSecretaryInvitation(SecretaryInvitationEmailModel model, CancellationToken ct = default);
        Task SendAppointmentCreatedToClient(AppointmentCreatedClientEmailModel model, CancellationToken ct = default);
        Task SendAppointmentCreatedToOwner(AppointmentCreatedOwnerEmailModel model, CancellationToken ct = default);
        Task SendAppointmentCancelledToClient(AppointmentCancelledClientEmailModel model, CancellationToken ct = default);
        Task SendAppointmentCancelledToOwner(AppointmentCancelledOwnerEmailModel model, CancellationToken ct = default);
        Task SendAppointmentRescheduledToClient(AppointmentRescheduledClientEmailModel model, CancellationToken ct = default);
        Task SendAppointmentRescheduledToOwner(AppointmentRescheduledOwnerEmailModel model, CancellationToken ct = default);
    }
}
