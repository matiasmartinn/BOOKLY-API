using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Application.Common.Models;
using AutoMapper;
using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Application.Common;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.DomainServices;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.Repositories;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using Microsoft.Extensions.Options;
using BOOKLY.Application.Services.ServiceTypeAggregate.DTOs;

namespace BOOKLY.Application.Services.ServiceAggregate
{
    public class ServiceApplicationService : IServiceApplicationService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IUserRepository _userRepository;
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceAuthorizationService _authorizationService;
        private readonly IAppointmentCancellationNotificationService _appointmentCancellationNotificationService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly FrontendOptions _frontendOptions;

        public ServiceApplicationService(
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IAvailabilityService availabilityService,
            IUserRepository userRepository,
            IServiceTypeRepository serviceTypeRepository,
            ISubscriptionRepository subscriptionRepository,
            IDateTimeProvider dateTimeProvider,
            IServiceAuthorizationService authorizationService,
            IAppointmentCancellationNotificationService appointmentCancellationNotificationService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOptions<FrontendOptions> frontendOptions
            )
        {
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _availabilityService = availabilityService;
            _userRepository = userRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _subscriptionRepository = subscriptionRepository;
            _dateTimeProvider = dateTimeProvider;
            _authorizationService = authorizationService;
            _appointmentCancellationNotificationService = appointmentCancellationNotificationService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _frontendOptions = frontendOptions.Value;
        }

        public async Task<Result<ServiceDto>> GetServiceById(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithSecretaries(id, ct);
            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            return Result<ServiceDto>.Success(await BuildServiceDto(service, ct));
        }

        public async Task<Result<List<ServiceDto>>> GetServicesByOwner(int ownerId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(ownerId, ct);
            if (user == null)
                return Result<List<ServiceDto>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwnerWithSecretaries(ownerId, ct);
            return Result<List<ServiceDto>>.Success(_mapper.Map<List<ServiceDto>>(services));
        }
        public async Task<Result<ServiceDto>> CreateService(CreateServiceDto dto, CancellationToken ct = default)
        {
            var owner = await _userRepository.GetOne(dto.OwnerId, ct);
            if (owner == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Usuario"));

            if (owner.Role != UserRole.Owner)
                return Result<ServiceDto>.Failure(Error.Validation("El usuario indicado no es un owner válido."));

            try
            {
                owner.EnsureCanLogin();
            }
            catch (DomainException)
            {
                return Result<ServiceDto>.Failure(Error.Validation("El owner debe estar habilitado para crear servicios."));
            }

            var serviceType = await _serviceTypeRepository.GetByIdWithFields(dto.ServiceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceDto>.Failure(Error.NotFound("TipoServicio"));

            var subscription = await GetEffectiveSubscription(dto.OwnerId, ct);
            var currentServices = await _serviceRepository.CountActiveByOwnerId(dto.OwnerId, ct);

            var subscriptionValidation = ValidateCanCreateService(subscription, currentServices);
            if (subscriptionValidation.IsFailure)
                return Result<ServiceDto>.Failure(subscriptionValidation.Error);

            var extraFieldsValidation = EnsureExtraFieldsAllowed(subscription, serviceType);
            if (extraFieldsValidation.IsFailure)
                return Result<ServiceDto>.Failure(extraFieldsValidation.Error);

            var slug = await GenerateUniqueSlugAsync(dto.Name, null, ct);
            Service service;

            try
            {
                var publicBookingCodeResult = await GenerateUniquePublicBookingCodeAsync(null, ct);
                if (publicBookingCodeResult.IsFailure)
                    return Result<ServiceDto>.Failure(publicBookingCodeResult.Error);

                service = Service.Create(
                    dto.Name,
                    dto.OwnerId,
                    slug,
                    dto.Description,
                    dto.PhoneNumber,
                    Location.Create(dto.PlaceName, dto.Address),
                    dto.ServiceTypeId,
                    _dateTimeProvider.NowArgentina(),
                    Duration.Create(dto.DurationMinutes),
                    Capacity.Create(dto.Capacity),
                    Mode.Presence,
                    dto.Price,
                    publicBookingCodeResult.Data
                );

                var schedulesResult = BuildSchedules(dto.Schedules, service.Capacity.Value);
                if (schedulesResult.IsFailure)
                    return Result<ServiceDto>.Failure(schedulesResult.Error);

                service.SetSchedules(schedulesResult.Data!);

                await _serviceRepository.AddOne(service);
                var saveResult = await SaveChangesEnsuringUniquePublicBookingCode(service, null, ct);
                if (saveResult.IsFailure)
                    return Result<ServiceDto>.Failure(saveResult.Error);
            }
            catch (ConflictException ex)
            {
                return Result<ServiceDto>.Failure(Error.Conflict(ex.Message));
            }
            catch (DomainException ex)
            {
                return Result<ServiceDto>.Failure(Error.Validation(ex.Message));
            }

            return Result<ServiceDto>.Success(await BuildServiceDto(service, ct));
        }

        public async Task<Result<ServiceDto>> UpdateService(int id, UpdateServiceDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);

            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            try
            {
                if (dto.Name != null)
                {
                    service.ChangeName(dto.Name);
                }
                if (dto.Slug != null)
                {
                    var slug = await GenerateUniqueSlugAsync(dto.Slug, id, ct);
                    service.ChangeSlug(slug);
                }
                if (dto.Description != null)
                {
                    service.ChangeDescription(dto.Description);
                }
                if (dto.PhoneNumber != null)
                {
                    service.ChangePhoneNumber(dto.PhoneNumber);
                }

                var isLocationBeingUpdated =
                    dto.PlaceName is not null ||
                    dto.Address is not null;

                if (isLocationBeingUpdated)
                {
                    service.ChangeLocation(
                        dto.PlaceName ?? service.Location?.PlaceName,
                        dto.Address ?? service.Location?.Address);
                }

                if (dto.DurationMinutes != null)
                {
                    service.ChangeDuration(dto.DurationMinutes.Value);
                }
                if (dto.Capacity != null)
                {
                    service.ChangeCapacity(dto.Capacity.Value);
                }

                if (dto.Price != null)
                {
                    service.ChangePrice(dto.Price.Value);
                }
            }
            catch (DomainException ex)
            {
                return Result<ServiceDto>.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(await BuildServiceDto(service, ct));
        }

        public async Task<Result> DeleteService(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);

            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.Deactivate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result<ServicePublicBookingDto>> GetPublicBooking(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<ServicePublicBookingDto>.Failure(Error.NotFound("Servicio"));

            return Result<ServicePublicBookingDto>.Success(MapPublicBooking(service));
        }

        public async Task<Result<ServicePublicBookingDto>> EnablePublicBooking(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<ServicePublicBookingDto>.Failure(Error.NotFound("Servicio"));

            var now = _dateTimeProvider.NowArgentina();

            try
            {
                string? publicBookingCode = null;
                if (string.IsNullOrWhiteSpace(service.PublicBookingCode))
                {
                    var publicBookingCodeResult = await GenerateUniquePublicBookingCodeAsync(service.Id, ct);
                    if (publicBookingCodeResult.IsFailure)
                        return Result<ServicePublicBookingDto>.Failure(publicBookingCodeResult.Error);

                    publicBookingCode = publicBookingCodeResult.Data;
                }

                service.EnablePublicBooking(now, publicBookingCode);

                _serviceRepository.Update(service);
                var saveResult = await SaveChangesEnsuringUniquePublicBookingCode(service, service.Id, ct);
                if (saveResult.IsFailure)
                    return Result<ServicePublicBookingDto>.Failure(saveResult.Error);
            }
            catch (ConflictException ex)
            {
                return Result<ServicePublicBookingDto>.Failure(Error.Conflict(ex.Message));
            }
            catch (DomainException ex)
            {
                return Result<ServicePublicBookingDto>.Failure(Error.Validation(ex.Message));
            }

            return Result<ServicePublicBookingDto>.Success(MapPublicBooking(service));
        }

        public async Task<Result<ServicePublicBookingDto>> DisablePublicBooking(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<ServicePublicBookingDto>.Failure(Error.NotFound("Servicio"));

            service.DisablePublicBooking();

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result<ServicePublicBookingDto>.Success(MapPublicBooking(service));
        }

        public async Task<Result<ServicePublicBookingDto>> RegeneratePublicBooking(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<ServicePublicBookingDto>.Failure(Error.NotFound("Servicio"));

            try
            {
                var publicBookingCodeResult = await GenerateUniquePublicBookingCodeAsync(service.Id, ct);
                if (publicBookingCodeResult.IsFailure)
                    return Result<ServicePublicBookingDto>.Failure(publicBookingCodeResult.Error);

                service.RegeneratePublicBookingCode(
                    _dateTimeProvider.NowArgentina(),
                    publicBookingCodeResult.Data);

                _serviceRepository.Update(service);
                var saveResult = await SaveChangesEnsuringUniquePublicBookingCode(service, service.Id, ct);
                if (saveResult.IsFailure)
                    return Result<ServicePublicBookingDto>.Failure(saveResult.Error);
            }
            catch (ConflictException ex)
            {
                return Result<ServicePublicBookingDto>.Failure(Error.Conflict(ex.Message));
            }
            catch (DomainException ex)
            {
                return Result<ServicePublicBookingDto>.Failure(Error.Validation(ex.Message));
            }

            return Result<ServicePublicBookingDto>.Success(MapPublicBooking(service));
        }

        public async Task<Result<List<ServiceScheduleDto>>> GetSchedulesByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<List<ServiceScheduleDto>>.Failure(Error.NotFound("Servicio"));

            var schedules = await _serviceRepository.GetSchedulesByService(serviceId, ct);

            return Result<List<ServiceScheduleDto>>.Success(_mapper.Map<List<ServiceScheduleDto>>(schedules));
        }

        public async Task<Result<ServiceDto>> SetSecretaries(int id, SetSecretariesDto dto, int? ownerScopeId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithSecretaries(id, ct);
            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            var secretaryIds = dto.SecretaryIds ?? [];

            foreach (var secretaryId in secretaryIds)
            {
                var secretary = await _userRepository.GetOne(secretaryId, ct);
                if (secretary == null || secretary.Role != UserRole.Secretary)
                    return Result<ServiceDto>.Failure(Error.Validation("Todos los usuarios asignados deben ser secretarios válidos."));
            }

            if (ownerScopeId.HasValue)
            {
                foreach (var secretaryId in secretaryIds)
                {
                    var secretaryOwnerIds = await _serviceRepository.GetOwnerIdsBySecretary(secretaryId, ct);
                    var belongsToAnotherOwner = secretaryOwnerIds.Count > 0 &&
                        secretaryOwnerIds.Any(ownerId => ownerId != ownerScopeId.Value);

                    if (belongsToAnotherOwner)
                    {
                        return Result<ServiceDto>.Failure(
                            Error.Forbidden("No puedes asignar secretarios que pertenecen a otro owner."));
                    }
                }
            }

            try
            {
                service.AssignSecretaries(secretaryIds);
            }
            catch (DomainException ex)
            {
                return Result<ServiceDto>.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(await BuildServiceDto(service, ct));
        }

        public async Task<Result> GrantSecretaryPermission(
            int serviceId,
            int secretaryId,
            SecretaryPermission permission,
            int currentUserId,
            UserRole currentUserRole,
            CancellationToken ct = default)
        {
            if (!Enum.IsDefined(typeof(SecretaryPermission), permission))
                return Result.Failure(Error.Validation("El permiso indicado no es válido."));

            var service = await _serviceRepository.GetOneWithSecretaries(serviceId, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            if (!CanManageSecretaryPermissions(service, currentUserId, currentUserRole))
                return Result.Failure(Error.Forbidden("No tiene permisos para administrar permisos de secretarios en este servicio."));

            var secretaryValidation = await ValidateSecretaryAssignmentAsync(service, secretaryId, ct);
            if (secretaryValidation.IsFailure)
                return secretaryValidation;

            try
            {
                service.GrantSecretaryPermission(secretaryId, permission);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result> RevokeSecretaryPermission(
            int serviceId,
            int secretaryId,
            SecretaryPermission permission,
            int currentUserId,
            UserRole currentUserRole,
            CancellationToken ct = default)
        {
            if (!Enum.IsDefined(typeof(SecretaryPermission), permission))
                return Result.Failure(Error.Validation("El permiso indicado no es válido."));

            var service = await _serviceRepository.GetOneWithSecretaries(serviceId, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            if (!CanManageSecretaryPermissions(service, currentUserId, currentUserRole))
                return Result.Failure(Error.Forbidden("No tiene permisos para administrar permisos de secretarios en este servicio."));

            var secretaryValidation = await ValidateSecretaryAssignmentAsync(service, secretaryId, ct);
            if (secretaryValidation.IsFailure)
                return secretaryValidation;

            try
            {
                service.RevokeSecretaryPermission(secretaryId, permission);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result<List<ScheduleUnavailabilityDto>>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<List<ScheduleUnavailabilityDto>>.Failure(Error.NotFound("Servicio"));

            var unavailability = await _serviceRepository.GetUnavailabilityByService(serviceId, ct);

            return Result<List<ScheduleUnavailabilityDto>>.Success(_mapper.Map<List<ScheduleUnavailabilityDto>>(unavailability));
        }

        public async Task<Result<ServiceDto>> SetSchedule(int id, List<CreateServiceScheduleDto> dto, CancellationToken ct = default)
        {
            var services = await _serviceRepository.GetOneWithSchedules(id, ct);

            if (services == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            var schedulesResult = BuildSchedules(dto, services.Capacity.Value);
            if (schedulesResult.IsFailure)
                return Result<ServiceDto>.Failure(schedulesResult.Error);

            try
            {
                services.SetSchedules(schedulesResult.Data!);
            }
            catch (DomainException ex)
            {
                return Result<ServiceDto>.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(services);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(await BuildServiceDto(services, ct));
        }

        // ========== UNAVAILABILITIES ==========

        public async Task<Result<List<ScheduleUnavailabilityDto>>> GetScheduleUnavailability(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result<List<ScheduleUnavailabilityDto>>.Failure(Error.NotFound("Servicio"));

            var dto = _mapper.Map<List<ScheduleUnavailabilityDto>>(service.ServicesUnavailability);
            return Result<List<ScheduleUnavailabilityDto>>.Success(dto);
        }
        public async Task<Result> AddUnavailability(int id, CreateUnavailabilityDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            var now = _dateTimeProvider.NowArgentina();
            var hasOnlyOneTime = dto.StartTime.HasValue != dto.EndTime.HasValue;
            if (hasOnlyOneTime)
                return Result.Failure(Error.Validation("Debe indicar hora de inicio y fin, o ninguna."));

            DateRange dateRange;
            ServiceUnavailability createdUnavailability;

            try
            {
                var today = DateOnly.FromDateTime(now);
                dateRange = DateRange.Create(dto.StartDate, dto.EndDate, today);

                var timeRange = dto.StartTime.HasValue && dto.EndTime.HasValue
                    ? TimeRange.Create(dto.StartTime.Value, dto.EndTime.Value)
                    : null;

                createdUnavailability = service.AddUnavailability(dateRange, timeRange, dto.Reason);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            var cancelledAppointments = new List<Appointment>();

            var candidateAppointments = await _appointmentRepository.GetPendingFutureByServiceAndDateRangeForUpdate(
                service.Id,
                dateRange.Start,
                dateRange.End,
                now,
                ct);

            var cancellationReason = BuildUnavailabilityCancellationReason(dto.Reason);
            var actorUserId = NormalizeActorUserId(dto.UserId);

            foreach (var appointment in candidateAppointments)
            {
                if (!IsAppointmentAffectedByUnavailability(appointment, createdUnavailability))
                    continue;

                try
                {
                    appointment.MarkAsCancel(cancellationReason, now, actorUserId);
                }
                catch (DomainException ex)
                {
                    return Result.Failure(Error.Validation(ex.Message));
                }

                cancelledAppointments.Add(appointment);
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            foreach (var appointment in cancelledAppointments)
            {
                await _appointmentCancellationNotificationService.NotifyAppointmentCancelled(
                    service,
                    appointment,
                    notifyOwner: false,
                    ct);
            }

            return Result.Success();
        }

        public async Task<Result> RemoveUnavailability(int id, int unavailabilityId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            try
            {
                service.RemoveUnavailability(unavailabilityId);
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        // ========== ACTIVATE / DEACTIVATE ==========
        public async Task<Result> Activate(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.Activate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }
        public async Task<Result> Deactivate(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.Deactivate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result<List<DateTime>>> GetAvailableSlots(int id, DateOnly date, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(id, ct);
            if (service == null)
                return Result<List<DateTime>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _appointmentRepository.GetByServiceAndDate(id, date, ct);

            var slots = _availabilityService.GetAvailableSlots(service, appointments, date, _dateTimeProvider.NowArgentina());
            return Result<List<DateTime>>.Success(slots.ToList());
        }

        public async Task<Result<List<DateOnly>>> GetAvailableDates(
            int id,
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
        {
            if (from > to)
                return Result<List<DateOnly>>.Failure(Error.Validation("El rango de fechas es inválido."));

            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(id, ct);
            if (service == null)
                return Result<List<DateOnly>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _appointmentRepository.GetByServiceAndDateRange(id, from, to, ct);

            var dates = _availabilityService.GetAvailableDates(service, appointments, from, to, _dateTimeProvider.NowArgentina());

            return Result<List<DateOnly>>.Success(dates.ToList());
        }

        private static Result<List<ServiceSchedule>> BuildSchedules(
            IEnumerable<CreateServiceScheduleDto> schedules,
            int defaultCapacity)
        {
            if (schedules is null)
                return Result<List<ServiceSchedule>>.Failure(Error.Validation("Debe proporcionar al menos un horario."));

            try
            {
                return Result<List<ServiceSchedule>>.Success(
                    schedules
                        .Select(s =>
                            ServiceSchedule.Create(
                                TimeRange.Create(s.StartTime, s.EndTime),
                                Capacity.Create(s.Capacity ?? defaultCapacity),
                                Day.Create(s.Day)
                            ))
                        .ToList());
            }
            catch (DomainException ex)
            {
                return Result<List<ServiceSchedule>>.Failure(Error.Validation(ex.Message));
            }
        }

        private async Task<Result> ValidateSecretaryAssignmentAsync(Service service, int secretaryId, CancellationToken ct)
        {
            var secretary = await _userRepository.GetOne(secretaryId, ct);
            if (secretary == null)
                return Result.Failure(Error.NotFound("Secretario"));

            if (secretary.Role != UserRole.Secretary)
                return Result.Failure(Error.Validation("El usuario indicado no es un secretario válido."));

            if (!service.ServiceSecretaries.Any(x => x.SecretaryId == secretaryId))
                return Result.Failure(Error.Validation("El secretario no pertenece al servicio."));

            return Result.Success();
        }

        private bool CanManageSecretaryPermissions(Service service, int currentUserId, UserRole currentUserRole)
        {
            if (currentUserRole == UserRole.Admin)
                return true;

            if (currentUserRole != UserRole.Owner)
                return false;

            return _authorizationService.HasPermission(
                service,
                currentUserId,
                currentUserRole,
                SecretaryPermission.ManageSchedules);
        }

        private static bool IsAppointmentAffectedByUnavailability(
            Appointment appointment,
            ServiceUnavailability unavailability)
        {
            var appointmentDate = DateOnly.FromDateTime(appointment.StartDateTime);
            var appointmentRange = TimeRange.Create(
                TimeOnly.FromDateTime(appointment.StartDateTime),
                TimeOnly.FromDateTime(appointment.EndDateTime));

            return unavailability.BlocksRange(appointmentDate, appointmentRange);
        }

        private static string BuildUnavailabilityCancellationReason(string? unavailabilityReason)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(unavailabilityReason)
                ? null
                : unavailabilityReason.Trim();

            return normalizedReason is null
                ? "Cancelado por excepción de agenda del servicio"
                : $"Cancelado por excepción de agenda: {normalizedReason}";
        }

        private static int? NormalizeActorUserId(int? userId)
            => userId.HasValue && userId.Value > 0 ? userId.Value : null;

        private async Task<string> GenerateUniqueSlugAsync(string source, int? excludedServiceId, CancellationToken ct)
        {
            var baseSlug = Slugify(source);
            var candidate = baseSlug;
            var suffix = 1;

            while (await _serviceRepository.ExistsSlug(candidate, excludedServiceId, ct))
            {
                suffix++;
                candidate = $"{baseSlug}-{suffix}";
            }

            return candidate;
        }

        private async Task<Subscription> GetEffectiveSubscription(int ownerId, CancellationToken ct)
        {
            var subscription = await _subscriptionRepository.GetByOwnerId(ownerId, ct);
            var today = DateOnly.FromDateTime(_dateTimeProvider.NowArgentina());

            if (subscription == null || !subscription.IsActive(today))
                return Subscription.CreateFree(ownerId, _dateTimeProvider.NowArgentina());

            return subscription;
        }

        private async Task<ServiceDto> BuildServiceDto(Service service, CancellationToken ct)
        {
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(service.ServiceTypeId, ct);
            var subscription = await GetEffectiveSubscription(service.OwnerId, ct);
            var orderedFields = serviceType?.FieldDefinitions
                .OrderBy(field => field.SortOrder)
                .ThenBy(field => field.Id)
                .ToList() ?? [];

            return _mapper.Map<ServiceDto>(service) with
            {
                AllowsExtraFields = subscription.Plan.AllowsExtraFields(),
                FieldDefinitions = _mapper.Map<List<ServiceTypeFieldDefinitionDto>>(orderedFields)
            };
        }

        private ServicePublicBookingDto MapPublicBooking(Service service)
        {
            return new ServicePublicBookingDto
            {
                ServiceId = service.Id,
                Slug = service.Slug.Value,
                IsEnabled = service.IsPublicBookingEnabled,
                PublicBookingCode = service.PublicBookingCode,
                PublicBookingCodeUpdatedAt = service.PublicBookingCodeUpdatedAt,
                PublicUrl = BuildPublicBookingUrl(service)
            };
        }

        private static Result ValidateCanCreateService(Subscription subscription, int currentServices)
        {
            try
            {
                subscription.EnsureCanCreateService(currentServices);
                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }
        }

        private static Result EnsureExtraFieldsAllowed(Subscription subscription, ServiceType serviceType)
        {
            try
            {
                if (serviceType.HasActiveFields())
                    subscription.EnsureCanUseExtraFields();

                return Result.Success();
            }
            catch (DomainException ex)
            {
                return Result.Failure(Error.Validation(ex.Message));
            }
        }

        private string BuildPublicBookingUrl(Service service)
        {
            var baseUrl = (_frontendOptions.BaseUrl ?? string.Empty).TrimEnd('/');
            var publicBookingPath = string.IsNullOrWhiteSpace(_frontendOptions.PublicBookingPath)
                ? "/book"
                : _frontendOptions.PublicBookingPath.Trim();

            if (!publicBookingPath.StartsWith('/'))
                publicBookingPath = $"/{publicBookingPath}";

            publicBookingPath = publicBookingPath.TrimEnd('/');

            return $"{baseUrl}{publicBookingPath}/{Uri.EscapeDataString(service.Slug.Value)}/{Uri.EscapeDataString(service.PublicBookingCode)}";
        }

        private async Task<Result<string>> GenerateUniquePublicBookingCodeAsync(int? excludedServiceId, CancellationToken ct)
        {
            const int maxAttempts = 10;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var publicBookingCode = Service.GeneratePublicBookingCode();

                if (!await _serviceRepository.ExistsPublicBookingCode(publicBookingCode, excludedServiceId, ct))
                    return Result<string>.Success(publicBookingCode);
            }

            return Result<string>.Failure(Error.Conflict("No se pudo generar un codigo publico unico para el servicio."));
        }

        private async Task<Result> SaveChangesEnsuringUniquePublicBookingCode(Service service, int? excludedServiceId, CancellationToken ct)
        {
            const int maxAttempts = 10;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    await _unitOfWork.SaveChanges(ct);
                    return Result.Success();
                }
                catch (ConflictException ex)
                {
                    if (!await _serviceRepository.ExistsPublicBookingCode(service.PublicBookingCode, excludedServiceId, ct))
                        return Result.Failure(Error.Conflict(ex.Message));

                    var publicBookingCodeResult = await GenerateUniquePublicBookingCodeAsync(excludedServiceId, ct);
                    if (publicBookingCodeResult.IsFailure)
                        return Result.Failure(publicBookingCodeResult.Error);

                    service.RegeneratePublicBookingCode(
                        _dateTimeProvider.NowArgentina(),
                        publicBookingCodeResult.Data);
                }
            }

            return Result.Failure(Error.Conflict("No se pudo persistir un codigo publico unico para el servicio."));
        }

        private static string Slugify(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                builder.Append(char.ToLowerInvariant(ch));
            }

            var slug = builder.ToString().Normalize(NormalizationForm.FormC);
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
                slug = "service";

            return slug;
        }
    }
}
