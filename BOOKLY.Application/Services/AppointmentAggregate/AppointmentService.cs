using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BOOKLY.Application.Services.AppointmentAggregate
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAppointmentHistoryRepository _historyRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceAuthorizationService _serviceAuthorizationService;

        public AppointmentService(
            IAppointmentRepository repository,
            IServiceRepository serviceRepository,
            IServiceTypeRepository serviceTypeRepository,
            IUserRepository userRepository,
            IAppointmentHistoryRepository historyRepository,
            IAvailabilityService availabilityService,
            IEmailService emailService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            IServiceAuthorizationService serviceAuthorizationService,
            ILogger<AppointmentService> logger)
        {
            _repository = repository;
            _serviceRepository = serviceRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _availabilityService = availabilityService;
            _emailService = emailService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _serviceAuthorizationService = serviceAuthorizationService;
        }

        public async Task<Result<AppointmentDto>> GetById(int id, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Turno"));

            return Result<AppointmentDto>.Success(_mapper.Map<AppointmentDto>(appointment));
        }

        public async Task<Result<IReadOnlyCollection<AppointmentDto>>> GetByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IReadOnlyCollection<AppointmentDto>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _repository.GetByService(serviceId, ct);
            return Result<IReadOnlyCollection<AppointmentDto>>.Success(
                _mapper.Map<IReadOnlyCollection<AppointmentDto>>(appointments));
        }

        public async Task<Result<IReadOnlyCollection<AppointmentSummaryDto>>> GetByServiceAndDate(
            int serviceId,
            DateOnly date,
            CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IReadOnlyCollection<AppointmentSummaryDto>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _repository.GetByServiceAndDate(serviceId, date, ct);
            return Result<IReadOnlyCollection<AppointmentSummaryDto>>.Success(
                _mapper.Map<IReadOnlyCollection<AppointmentSummaryDto>>(appointments));
        }

        public async Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> GetByDay(AppointmentDayQueryDto dto, CancellationToken ct = default)
        {
            var scopedServicesResult = await ResolveScopedServices(dto.OwnerId, dto.ServiceId, ct);
            if (scopedServicesResult.IsFailure)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(scopedServicesResult.Error!);

            var services = scopedServicesResult.Data!;
            if (services.Count == 0)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Success([]);

            var appointments = await _repository.SearchByServices(
                services.Select(service => service.Id).ToList(),
                dto.Date,
                dto.Date,
                null,
                null,
                null,
                orderDescending: false,
                ct);

            var serviceNames = services.ToDictionary(service => service.Id, service => service.Name);
            var result = appointments
                .Select(appointment => MapListItemDto(
                    appointment,
                    serviceNames.GetValueOrDefault(appointment.ServiceId, string.Empty)))
                .ToList();

            return Result<IReadOnlyCollection<AppointmentListItemDto>>.Success(result);
        }

        public async Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> SearchAppointments(AppointmentQueryDto dto, CancellationToken ct = default)
        {
            if (dto.From.HasValue && dto.To.HasValue && dto.From.Value > dto.To.Value)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(
                    Error.Validation("El rango de fechas es inválido."));

            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                !Enum.TryParse<AppointmentStatus>(dto.Status, true, out _))
            {
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(
                    Error.Validation("El estado indicado no es válido."));
            }

            var scopedServicesResult = await ResolveScopedServices(dto.OwnerId, dto.ServiceId, ct);
            if (scopedServicesResult.IsFailure)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(scopedServicesResult.Error!);

            var services = scopedServicesResult.Data!;
            if (services.Count == 0)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Success([]);

            AppointmentStatus? status = null;
            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                Enum.TryParse<AppointmentStatus>(dto.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }

            var appointments = await _repository.SearchByServices(
                services.Select(service => service.Id).ToList(),
                dto.From,
                dto.To,
                status,
                dto.ClientSearch,
                dto.ClientEmail,
                orderDescending: true,
                ct);

            var serviceNames = services.ToDictionary(service => service.Id, service => service.Name);
            var filtered = appointments
                .Select(appointment => MapListItemDto(
                    appointment,
                    serviceNames.GetValueOrDefault(appointment.ServiceId, string.Empty)))
                .ToList();

            return Result<IReadOnlyCollection<AppointmentListItemDto>>.Success(filtered);
        }

        public async Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByAppointment(int id, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Failure(Error.NotFound("Turno"));

            var history = await _historyRepository.GetByAppointment(id, ct);
            return Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Success(
                _mapper.Map<IReadOnlyCollection<AppointmentStatusHistoryDto>>(history));
        }

        public async Task<Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>> GetHistoryByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Failure(Error.NotFound("Servicio"));

            var history = await _historyRepository.GetByService(serviceId, ct);
            return Result<IReadOnlyCollection<AppointmentStatusHistoryDto>>.Success(
                _mapper.Map<IReadOnlyCollection<AppointmentStatusHistoryDto>>(history));
        }

        public async Task<Result<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();

            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(dto.ServiceId, ct);
            if (service == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Servicio"));

            var serviceType = await _serviceTypeRepository.GetByIdWithFields(service.ServiceTypeId, ct);
            if (serviceType == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("TipoServicio"));

            var fieldValidation = ValidateFieldValues(dto.FieldValues, serviceType);
            if (fieldValidation.IsFailure)
                return Result<AppointmentDto>.Failure(fieldValidation.Error!);

            var slotValidation = await ValidateSlotAvailability(service, dto.StartDateTime, null, ct);
            if (slotValidation.IsFailure)
                return Result<AppointmentDto>.Failure(slotValidation.Error!);

            var appointment = Appointment.Create(
                dto.ServiceId,
                dto.AssignedSecretaryId,
                ClientInfo.Create(dto.ClientName, dto.ClientPhone, Email.Create(dto.ClientEmail)),
                dto.StartDateTime,
                service.DurationMinutes,
                dto.ClientNotes,
                now,
                NormalizeActorUserId(dto.UserId));

            foreach (var fv in dto.FieldValues)
                appointment.AddFieldValue(fv.FieldDefinitionId, fv.Value);

            await _repository.AddOne(appointment, ct);
            await _unitOfWork.SaveChanges(ct);

            await NotifyAppointmentCreated(service, appointment, ct);

            return Result<AppointmentDto>.Success(_mapper.Map<AppointmentDto>(appointment));
        }

        public async Task<Result<AppointmentDto>> UpdateAppointmentInformation(int id, UpdateAppointmentDto dto, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Turno"));

            appointment.ChangeClientInfo(ClientInfo.Create(dto.ClientName, dto.ClientPhone, Email.Create(dto.ClientEmail)), now);
            appointment.ChangeClientNotes(dto.ClientNotes, now);

            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result<AppointmentDto>.Success(_mapper.Map<AppointmentDto>(appointment));
        }

        public async Task<Result<AppointmentDto>> RescheduleAppointment(int id, RescheduleAppointmentDto dto, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Turno"));

            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(appointment.ServiceId, ct);
            if (service == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Servicio"));

            var slotValidation = await ValidateSlotAvailability(service, dto.StartDateTime, appointment.Id, ct);
            if (slotValidation.IsFailure)
                return Result<AppointmentDto>.Failure(slotValidation.Error!);

            var previousStartDateTime = appointment.StartDateTime;
            appointment.Reschedule(dto.StartDateTime, service.DurationMinutes, now);
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);

            await NotifyAppointmentRescheduled(service, appointment, previousStartDateTime, ct);

            return Result<AppointmentDto>.Success(_mapper.Map<AppointmentDto>(appointment));
        }

        public async Task<Result> MarkAsCancel(int id, CancelAppointmentDto dto, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(appointment.ServiceId, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            appointment.MarkAsCancel(dto.Reason, now, NormalizeActorUserId(dto.UserId));
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);

            await NotifyAppointmentCancelled(service, appointment, ct);

            return Result.Success();
        }

        public async Task<Result> MarkAsAttended(int id, int? userId = null, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.MarkAsAttended(now, NormalizeActorUserId(userId));
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result> MarkAsNoShow(int id, int? userId = null, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.MarkAsNoShow(now, NormalizeActorUserId(userId));
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        private async Task<Result> ValidateSlotAvailability(
            Service service,
            DateTime requestedStart,
            int? excludedAppointmentId,
            CancellationToken ct)
        {
            var now = _dateTimeProvider.NowArgentina();
            if (!service.IsActive)
                return Result.Failure(Error.Conflict("El servicio no se encuentra activo."));

            if (requestedStart <= now)
                return Result.Failure(Error.Validation("El turno debe agendarse en un horario futuro."));

            if (service.GetScheduleFor(requestedStart, service.DurationMinutes) is null)
                return Result.Failure(Error.Validation("El horario no pertenece a la agenda del servicio."));

            var date = DateOnly.FromDateTime(requestedStart);
            var appointments = await _repository.GetByServiceAndDate(service.Id, date, ct);
            var appointmentsToEvaluate = excludedAppointmentId.HasValue
                ? appointments.Where(a => a.Id != excludedAppointmentId.Value).ToList()
                : appointments;

            var availableSlots = _availabilityService.GetAvailableSlots(service, appointmentsToEvaluate, date, now);

            if (!availableSlots.Contains(requestedStart))
                return Result.Failure(Error.Conflict("El horario seleccionado ya no está disponible."));

            return Result.Success();
        }

        private async Task NotifyAppointmentCreated(Service service, Appointment appointment, CancellationToken ct)
        {
            var owner = await _userRepository.GetOne(service.OwnerId, ct);
            var businessName = owner is null
                ? "BOOKLY"
                : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

            await TrySendEmail(
                () => _emailService.SendAppointmentCreatedToClient(
                    new AppointmentCreatedClientEmailModel(
                        appointment.Client.Email.Value,
                        appointment.Client.ClientName,
                        service.Name,
                        businessName,
                        appointment.StartDateTime,
                        appointment.Duration.Value),
                    ct),
                "confirmación de turno al cliente",
                appointment.Client.Email.Value);

            if (owner is null)
                return;

            await TrySendEmail(
                () => _emailService.SendAppointmentCreatedToOwner(
                    new AppointmentCreatedOwnerEmailModel(
                        owner.Email.Value,
                        owner.PersonName.FirstName,
                        appointment.Client.ClientName,
                        appointment.Client.Email.Value,
                        appointment.Client.Phone,
                        service.Name,
                        appointment.StartDateTime,
                        appointment.Duration.Value),
                    ct),
                "notificación de turno al owner",
                owner.Email.Value);
        }

        private async Task NotifyAppointmentCancelled(Service service, Appointment appointment, CancellationToken ct)
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
                "cancelación de turno al cliente",
                appointment.Client.Email.Value);

            if (owner is null)
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
                "cancelación de turno al owner",
                owner.Email.Value);
        }

        private async Task NotifyAppointmentRescheduled(
            Service service,
            Appointment appointment,
            DateTime previousStartDateTime,
            CancellationToken ct)
        {
            var owner = await _userRepository.GetOne(service.OwnerId, ct);
            var businessName = owner is null
                ? "BOOKLY"
                : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}";

            await TrySendEmail(
                () => _emailService.SendAppointmentRescheduledToClient(
                    new AppointmentRescheduledClientEmailModel(
                        appointment.Client.Email.Value,
                        appointment.Client.ClientName,
                        service.Name,
                        businessName,
                        previousStartDateTime,
                        appointment.StartDateTime,
                        appointment.Duration.Value),
                    ct),
                "reprogramación de turno al cliente",
                appointment.Client.Email.Value);

            if (owner is null)
                return;

            await TrySendEmail(
                () => _emailService.SendAppointmentRescheduledToOwner(
                    new AppointmentRescheduledOwnerEmailModel(
                        owner.Email.Value,
                        owner.PersonName.FirstName,
                        appointment.Client.ClientName,
                        appointment.Client.Email.Value,
                        appointment.Client.Phone,
                        service.Name,
                        previousStartDateTime,
                        appointment.StartDateTime,
                        appointment.Duration.Value),
                    ct),
                "reprogramación de turno al owner",
                owner.Email.Value);
        }

        private async Task<Result<List<Service>>> ResolveScopedServices(int? ownerId, int? serviceId, CancellationToken ct)
        {
            if (serviceId.HasValue)
            {
                var service = await _serviceRepository.GetOne(serviceId.Value, ct);
                if (service == null)
                    return Result<List<Service>>.Failure(Error.NotFound("Servicio"));

                if (ownerId.HasValue && service.OwnerId != ownerId.Value)
                    return Result<List<Service>>.Failure(Error.Validation("El servicio no pertenece al owner indicado."));

                return Result<List<Service>>.Success([service]);
            }

            if (!ownerId.HasValue || ownerId.Value <= 0)
                return Result<List<Service>>.Failure(Error.Validation("Debe indicar ownerId o serviceId."));

            var owner = await _userRepository.GetOne(ownerId.Value, ct);
            if (owner == null || owner.Role != UserRole.Owner)
                return Result<List<Service>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwner(ownerId.Value, ct);
            return Result<List<Service>>.Success(services);
        }

        private static int? NormalizeActorUserId(int? userId)
            => userId.HasValue && userId.Value > 0 ? userId.Value : null;

        private AppointmentListItemDto MapListItemDto(Appointment appointment, string serviceName)
        {
            return _mapper.Map<AppointmentListItemDto>(appointment, options =>
            {
                options.Items[AppointmentMappingProfile.ServiceNameContextKey] = serviceName;
            });
        }

        // Las notificaciones de email son complementarias y no deben revertir la operación principal.
        private static Result ValidateFieldValues(
            IReadOnlyCollection<CreateAppointmentFieldValueDto> fieldValues,
            ServiceType serviceType)
        {
            var activeFields = serviceType.FieldDefinitions
                .Where(field => field.IsActive)
                .ToList();

            if (fieldValues.GroupBy(field => field.FieldDefinitionId).Any(group => group.Count() > 1))
                return Result.Failure(Error.Validation("No se puede informar más de un valor por cada campo adicional."));

            if (activeFields.Count == 0)
            {
                if (fieldValues.Count > 0)
                    return Result.Failure(Error.Validation("El servicio no admite campos adicionales."));

                return Result.Success();
            }

            foreach (var fieldValue in fieldValues)
            {
                var field = activeFields.FirstOrDefault(definition => definition.Id == fieldValue.FieldDefinitionId);
                if (field == null)
                    return Result.Failure(Error.Validation("Uno o más campos enviados no pertenecen al tipo de servicio."));

                if (string.IsNullOrWhiteSpace(fieldValue.Value))
                    return Result.Failure(Error.Validation($"El campo '{field.Label.Value}' no puede estar vacío."));

                if (!IsFieldValueValid(field, fieldValue.Value))
                    return Result.Failure(Error.Validation(BuildInvalidFieldValueMessage(field)));
            }

            foreach (var requiredField in activeFields.Where(field => field.IsRequired))
            {
                var hasValue = fieldValues.Any(fieldValue =>
                    fieldValue.FieldDefinitionId == requiredField.Id &&
                    !string.IsNullOrWhiteSpace(fieldValue.Value));

                if (!hasValue)
                    return Result.Failure(Error.Validation($"El campo '{requiredField.Label.Value}' es obligatorio."));
            }

            return Result.Success();
        }

        private static bool IsFieldValueValid(ServiceTypeFieldDefinition field, string value)
        {
            var trimmedValue = value.Trim();

            return field.FieldType switch
            {
                ServiceFieldType.Text => true,
                ServiceFieldType.MultilineText => true,
                ServiceFieldType.Number => decimal.TryParse(
                    trimmedValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out _) || decimal.TryParse(trimmedValue, out _),
                ServiceFieldType.Date => DateOnly.TryParse(trimmedValue, out _) || DateTime.TryParse(trimmedValue, out _),
                ServiceFieldType.Select => field.Options.Any(option =>
                    option.IsActive &&
                    string.Equals(option.Value, trimmedValue, StringComparison.OrdinalIgnoreCase)),
                ServiceFieldType.Checkbox => bool.TryParse(trimmedValue, out _),
                _ => false
            };
        }

        private static string BuildInvalidFieldValueMessage(ServiceTypeFieldDefinition field)
        {
            return field.FieldType switch
            {
                ServiceFieldType.Number => $"El campo '{field.Label.Value}' debe contener un número válido.",
                ServiceFieldType.Date => $"El campo '{field.Label.Value}' debe contener una fecha válida.",
                ServiceFieldType.Select => $"El campo '{field.Label.Value}' debe seleccionar una opción válida.",
                ServiceFieldType.Checkbox => $"El campo '{field.Label.Value}' debe indicar true o false.",
                _ => $"El campo '{field.Label.Value}' contiene un valor inválido."
            };
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
                    "El turno se guardó correctamente, pero ocurrió un error inesperado enviando el email de {Purpose} a {RecipientEmail}.",
                    purpose,
                    recipientEmail);
            }
        }
    }
}
