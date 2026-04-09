using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.AppointmentAggregate
{
    public sealed class AppointmentCancellationNotificationService : IAppointmentCancellationNotificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<AppointmentCancellationNotificationService> _logger;

        public AppointmentCancellationNotificationService(
            IUserRepository userRepository,
            IEmailService emailService,
            ILogger<AppointmentCancellationNotificationService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifyAppointmentCancelled(
            Service service,
            Appointment appointment,
            bool notifyOwner,
            CancellationToken ct = default)
        {
            var owner = await _userRepository.GetOne(service.OwnerId, ct);
            var businessName = owner is null
                ? "BOOKLY"
                : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

            await TrySendEmail(
                () => _emailService.SendAppointmentCancelledToClient(
                    new AppointmentCancelledClientEmailModel(
                        appointment.Client.Email.Value,
                        appointment.Client.ClientName,
                        service.Name,
                        businessName,
                        appointment.StartDateTime,
                        appointment.CancelReason),
                    ct),
                "cancelaciÃ³n de turno al cliente",
                appointment.Client.Email.Value);

            if (!notifyOwner || owner is null)
                return;

            await TrySendEmail(
                () => _emailService.SendAppointmentCancelledToOwner(
                    new AppointmentCancelledOwnerEmailModel(
                        owner.Email.Value,
                        owner.PersonName.FirstName,
                        appointment.Client.ClientName,
                        appointment.Client.Email.Value,
                        appointment.Client.Phone,
                        service.Name,
                        appointment.StartDateTime,
                        appointment.CancelReason),
                    ct),
                "cancelaciÃ³n de turno al owner",
                owner.Email.Value);
        }

        private async Task TrySendEmail(Func<Task> sendEmail, string purpose, string recipientEmail)
        {
            try
            {
                await sendEmail();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "El turno se guardÃ³ correctamente, pero ocurriÃ³ un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                    purpose,
                    recipientEmail);
            }
        }
    }
}
