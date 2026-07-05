using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.AppointmentAggregate
{
    public sealed class AppointmentCancellationNotificationService : IAppointmentCancellationNotificationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ISafeEmailDispatcher _safeEmailDispatcher;

        public AppointmentCancellationNotificationService(
            IUserRepository userRepository,
            IEmailService emailService,
            ISafeEmailDispatcher safeEmailDispatcher)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _safeEmailDispatcher = safeEmailDispatcher;
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

            await _safeEmailDispatcher.TrySend(
                () => _emailService.SendAppointmentCancelledToClient(
                    new AppointmentCancelledClientEmailModel(
                        appointment.Client.Email.Value,
                        appointment.Client.ClientName,
                        service.Name,
                        businessName,
                        appointment.StartDateTime,
                        appointment.CancelReason),
                    ct),
                "cancelación de turno al cliente",
                appointment.Client.Email.Value);

            if (!notifyOwner || owner is null)
                return;

            await _safeEmailDispatcher.TrySend(
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
                "cancelación de turno al owner",
                owner.Email.Value);
        }
    }
}
