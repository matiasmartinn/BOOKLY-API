using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Domain.Emailing;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

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
        private readonly IAppointmentCancellationNotificationService _appointmentCancellationNotificationService;
        private readonly IEffectiveSubscriptionResolver _effectiveSubscriptionResolver;
        private readonly IAppointmentValidator _appointmentValidator;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AppointmentService(
            IAppointmentRepository repository,
            IServiceRepository serviceRepository,
            IServiceTypeRepository serviceTypeRepository,
            IUserRepository userRepository,
            IAppointmentHistoryRepository historyRepository,
            IAvailabilityService availabilityService,
            IEmailService emailService,
            IAppointmentCancellationNotificationService appointmentCancellationNotificationService,
            IEffectiveSubscriptionResolver effectiveSubscriptionResolver,
            IAppointmentValidator appointmentValidator,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider,
            ILogger<AppointmentService> logger)
        {
            _repository = repository;
            _serviceRepository = serviceRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _userRepository = userRepository;
            _historyRepository = historyRepository;
            _availabilityService = availabilityService;
            _emailService = emailService;
            _appointmentCancellationNotificationService = appointmentCancellationNotificationService;
            _effectiveSubscriptionResolver = effectiveSubscriptionResolver;
            _appointmentValidator = appointmentValidator;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
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
            var result = await MapListItemDtos(appointments, serviceNames, ct);

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
            var filtered = await MapListItemDtos(appointments, serviceNames, ct);

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
            var requestedStart = dto.StartDateTime;

            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(dto.ServiceId, ct);
            if (service == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Servicio"));

            var serviceType = await _serviceTypeRepository.GetByIdWithFields(service.ServiceTypeId, ct);
            if (serviceType == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("TipoServicio"));

            var extraFieldsValidation = await ValidateExtraFieldsAllowed(service.OwnerId, ct);
            var canUseExtraFields = extraFieldsValidation.IsSuccess;

            if (!canUseExtraFields && dto.FieldValues.Count > 0)
                return Result<AppointmentDto>.Failure(extraFieldsValidation.Error!);

            if (canUseExtraFields)
            {
                var fieldValidation = _appointmentValidator.Validate(dto.FieldValues, serviceType);
                if (fieldValidation.IsFailure)
                    return Result<AppointmentDto>.Failure(fieldValidation.Error!);
            }

            var slotValidation = await ValidateSlotAvailability(
                service,
                requestedStart,
                null,
                requireActiveService: true,
                ct);
            if (slotValidation.IsFailure)
                return Result<AppointmentDto>.Failure(slotValidation.Error!);

            Appointment appointment;
            try
            {
                appointment = Appointment.Create(
                    dto.ServiceId,
                    dto.AssignedSecretaryId,
                    ClientInfo.Create(dto.ClientName, dto.ClientPhone, Email.Create(dto.ClientEmail)),
                    requestedStart,
                    service.DurationMinutes,
                    dto.ClientNotes,
                    now,
                    NormalizeActorUserId(dto.UserId));

                if (canUseExtraFields)
                {
                    foreach (var fv in dto.FieldValues)
                        appointment.AddFieldValue(fv.FieldDefinitionId, fv.Value);
                }
            }
            catch (DomainException ex)
            {
                return Result<AppointmentDto>.Failure(Error.Validation(ex.Message));
            }

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

            try
            {
                appointment.ChangeClientInfo(ClientInfo.Create(dto.ClientName, dto.ClientPhone, Email.Create(dto.ClientEmail)), now);
                appointment.ChangeClientNotes(dto.ClientNotes, now);
            }
            catch (DomainException ex)
            {
                return Result<AppointmentDto>.Failure(Error.Validation(ex.Message));
            }

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

            var slotValidation = await ValidateSlotAvailability(
                service,
                dto.StartDateTime,
                appointment.Id,
                requireActiveService: false,
                ct);
            if (slotValidation.IsFailure)
                return Result<AppointmentDto>.Failure(slotValidation.Error!);

            var previousStartDateTime = appointment.StartDateTime;
            try
            {
                appointment.Reschedule(dto.StartDateTime, service.DurationMinutes, now);
            }
            catch (DomainException ex)
            {
                return Result<AppointmentDto>.Failure(Error.Validation(ex.Message));
            }

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

            try
            {
                appointment.MarkAsCancel(dto.Reason, now, NormalizeActorUserId(dto.UserId));
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);

            await _appointmentCancellationNotificationService.NotifyAppointmentCancelled(
                service,
                appointment,
                notifyOwner: true,
                ct);

            return Result.Success();
        }

        public async Task<Result> MarkAsAttended(int id, int? userId = null, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            try
            {
                appointment.MarkAsAttended(now, NormalizeActorUserId(userId));
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result<ResolveExpiredAppointmentsResultDto>> ResolveExpiredPendingAppointments(
            int ownerId,
            int? userId = null,
            CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var startOfToday = now.Date;

            var services = await _serviceRepository.GetServicesByOwner(ownerId, ct);
            var autoCloseServiceIds = services
                .Where(s => s.AttendanceClosingMode == AttendanceClosingMode.AutoMarkAsAttended)
                .Select(s => s.Id)
                .ToList();

            if (autoCloseServiceIds.Count == 0)
                return Result<ResolveExpiredAppointmentsResultDto>.Success(
                    new ResolveExpiredAppointmentsResultDto(0));

            var expiredAppointments = await _repository.GetExpiredPendingByServices(autoCloseServiceIds, startOfToday, ct);

            try
            {
                foreach (var appointment in expiredAppointments)
                {
                    appointment.MarkAsAttended(now, NormalizeActorUserId(userId));
                    _repository.Update(appointment);
                }
            }
            catch (DomainException ex)
            {
                return Result<ResolveExpiredAppointmentsResultDto>.Failure(Error.Validation(ex.Message));
            }

            if (expiredAppointments.Count > 0)
                await _unitOfWork.SaveChanges(ct);

            return Result<ResolveExpiredAppointmentsResultDto>.Success(
                new ResolveExpiredAppointmentsResultDto(expiredAppointments.Count));
        }

        public async Task<Result> MarkAsNoShow(int id, int? userId = null, CancellationToken ct = default)
        {
            var now = _dateTimeProvider.NowArgentina();
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            try
            {
                appointment.MarkAsNoShow(now, NormalizeActorUserId(userId));
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        private async Task<Result> ValidateSlotAvailability(
            Service service,
            DateTime requestedStart,
            int? excludedAppointmentId,
            bool requireActiveService,
            CancellationToken ct)
        {
            var now = _dateTimeProvider.NowArgentina();
            if (requireActiveService && !service.IsActive)
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

        private async Task<List<AppointmentListItemDto>> MapListItemDtos(
            IReadOnlyCollection<Appointment> appointments,
            IReadOnlyDictionary<int, string> serviceNames,
            CancellationToken ct)
        {
            if (appointments.Count == 0)
                return [];

            var creationEntries = await _historyRepository.GetCreationEntriesByAppointments(
                appointments.Select(appointment => appointment.Id).ToArray(),
                ct);

            var creationEntryByAppointmentId = creationEntries
                .GroupBy(entry => entry.AppointmentId)
                .ToDictionary(group => group.Key, group => group.First());

            return appointments
                .Select(appointment =>
                {
                    creationEntryByAppointmentId.TryGetValue(appointment.Id, out var creationEntry);
                    serviceNames.TryGetValue(appointment.ServiceId, out var serviceName);

                    return MapListItemDto(
                        appointment,
                        serviceName ?? string.Empty,
                        creationEntry);
                })
                .ToList();
        }

        private AppointmentListItemDto MapListItemDto(
            Appointment appointment,
            string serviceName,
            AppointmentStatusHistory? creationEntry = null)
        {
            var dto = _mapper.Map<AppointmentListItemDto>(appointment, options =>
            {
                options.Items[AppointmentMappingProfile.ServiceNameContextKey] = serviceName;
            });

            return dto with
            {
                CreatedByUserId = creationEntry?.UserId,
                CreatedByUserDisplayName = creationEntry?.User is null
                    ? null
                    : $"{creationEntry.User.PersonName.FirstName} {creationEntry.User.PersonName.LastName}".Trim(),
                CreatedByUserRole = creationEntry?.User?.Role.ToString()
            };
        }

        // Las notificaciones de email son complementarias y no deben revertir la operación principal.
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

        private async Task<Result> ValidateExtraFieldsAllowed(int ownerId, CancellationToken ct)
        {
            var subscription = await _effectiveSubscriptionResolver.Resolve(ownerId, ct);

            try
            {
                subscription.EnsureCanUseExtraFields();
                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }
        }
    }
}
