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
using BOOKLY.Domain.DomainServices;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BOOKLY.Domain.Aggregates.SubscriptionAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Repositories;

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
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ServiceApplicationService(
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IAvailabilityService availabilityService,
            IUserRepository userRepository,
            IServiceTypeRepository serviceTypeRepository,
            ISubscriptionRepository subscriptionRepository,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper,
            IUnitOfWork unitOfWork
            ) 
        {
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _availabilityService = availabilityService;
            _userRepository = userRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _subscriptionRepository = subscriptionRepository;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ServiceDto>> GetServiceById(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
        }

        public async Task<Result<List<ServiceDto>>> GetServicesByOwner(int ownerId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(ownerId, ct);
            if (user == null)
                return Result<List<ServiceDto>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwner(ownerId, ct);
            if (!services.Any())
                return Result<List<ServiceDto>>.Failure(Error.NotFound("Services"));

            return Result<List<ServiceDto>>.Success(_mapper.Map<List<ServiceDto>>(services));
        }
        public async Task<Result<ServiceDto>> CreateService(CreateServiceDto dto, CancellationToken ct = default)
        {
            var owner = await _userRepository.GetOne(dto.OwnerId, ct);
            if (owner == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Usuario"));

            var serviceType = await _serviceTypeRepository.GetByIdWithFields(dto.ServiceTypeId, ct);
            if (serviceType == null)
                return Result<ServiceDto>.Failure(Error.NotFound("TipoServicio"));

            var subscription = await GetEffectiveSubscription(dto.OwnerId, ct);
            var currentServices = await _serviceRepository.CountByOwnerId(dto.OwnerId, ct);

            subscription.EnsureCanCreateService(currentServices);
            EnsureExtraFieldsAllowed(subscription, serviceType);

            var slug = await GenerateUniqueSlugAsync(dto.Name, null, ct);

            var service = Service.Create(
                dto.Name,
                dto.OwnerId,
                slug,
                dto.Description,
                Location.Create(dto.PlaceName, dto.Address,dto.GoogleMapsUrl),
                dto.ServiceTypeId,
                Duration.Create(dto.DurationMinutes),
                Capacity.Create(dto.Capacity),
                Mode.Presence,
                dto.Price
            );

            var schedules = BuildSchedules(dto.Schedules, service.Capacity.Value);

            service.SetSchedules(schedules);

            await _serviceRepository.AddOne(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
        }

        public async Task<Result<ServiceDto>> UpdateService(int id, UpdateServiceDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);

            if(service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            ServiceType? nextServiceType = null;
            if (dto.ServiceTypeId.HasValue)
            {
                nextServiceType = await _serviceTypeRepository.GetOne(dto.ServiceTypeId.Value, ct);
                if (nextServiceType == null)
                    return Result<ServiceDto>.Failure(Error.NotFound("TipoServicio"));
            }

            var subscription = await GetEffectiveSubscription(service.OwnerId, ct);

            if(dto.Name != null) {
                service.ChangeName(dto.Name);
            }
            if(dto.Slug != null) {
                var slug = await GenerateUniqueSlugAsync(dto.Slug, id, ct);
                service.ChangeSlug(slug);
            }
            if(!string.IsNullOrWhiteSpace(dto.Description)) {
                service.ChangeDescription(dto.Description);
            }

            var isLocationBeingUpdated =
                dto.PlaceName is not null ||
                dto.Address is not null ||
                dto.GoogleMapsUrl is not null;

            if (isLocationBeingUpdated)
            {
                service.ChangeLocation(
                    dto.PlaceName ?? service?.Location.PlaceName,
                    dto.Address ?? service?.Location.Address,
                    dto.GoogleMapsUrl ?? service?.Location.GoogleMapsUrl);
            }

            if (dto.DurationMinutes != null ) {
                service.ChangeDuration(dto.DurationMinutes.Value);
            }
            if(dto.Capacity != null) {
                service.ChangeCapacity(dto.Capacity.Value);
            }

            if (dto.ServiceTypeId.HasValue)
            {
                EnsureExtraFieldsAllowed(subscription, nextServiceType!);
                service.ChangeServiceType(dto.ServiceTypeId.Value);
            }

            if(dto.Price != null){
                service.ChangePrice(dto.Price.Value);
            }

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
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

        public async Task<Result<List<ServiceScheduleDto>>> GetSchedulesByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<List<ServiceScheduleDto>>.Failure(Error.NotFound("Servicio"));

            var schedules = await _serviceRepository.GetSchedulesByService(serviceId, ct);
            
            return Result<List<ServiceScheduleDto>>.Success(_mapper.Map<List<ServiceScheduleDto>>(schedules));
        }

        public async Task<Result<ServiceDto>> SetSecretaries(int id, SetSecretariesDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithSecretaries(id, ct);
            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            var secretaryIds = dto.SecretaryIds ?? [];

            foreach (var secretaryId in secretaryIds)
            {
                var secretary = await _userRepository.GetOne(secretaryId, ct);
                if (secretary == null || secretary.Role != Domain.Aggregates.UserAggregate.UserKind.Secretary)
                    return Result<ServiceDto>.Failure(Error.Validation("Todos los usuarios asignados deben ser secretarios válidos."));
            }

            service.AssignSecretaries(secretaryIds);

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
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

            var schedules = BuildSchedules(dto, services.Capacity.Value);
            services.SetSchedules(schedules);
            
            _serviceRepository.Update(services);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(services));
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

            var hasOnlyOneTime = dto.StartTime.HasValue != dto.EndTime.HasValue;
            if (hasOnlyOneTime)
                return Result.Failure(Error.Validation("Debe indicar hora de inicio y fin, o ninguna."));

            var dateRange = DateRange.Create(dto.StartDate, dto.EndDate);

            var timeRange = dto.StartTime.HasValue && dto.EndTime.HasValue
                ? TimeRange.Create(dto.StartTime.Value, dto.EndTime.Value)
                : null;

            service.AddUnavailability(dateRange, timeRange, dto.Reason);

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> RemoveUnavailability(int id, int unavailabilityId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.RemoveUnavailability(unavailabilityId);

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

            var appointments= await _appointmentRepository.GetByServiceAndDate(id, date, ct);

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

        private static List<ServiceSchedule> BuildSchedules(
            IEnumerable<CreateServiceScheduleDto> schedules,
            int defaultCapacity)
        {
            return schedules
                .Select(s =>
                    ServiceSchedule.Create(
                        TimeRange.Create(s.StartTime, s.EndTime),
                        Capacity.Create(s.Capacity ?? defaultCapacity),
                        Day.Create(s.Day)
                    ))
                .ToList();
        }

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

        private static void EnsureExtraFieldsAllowed(Subscription subscription, ServiceType serviceType)
        {
            if (serviceType.HasActiveFields())
                subscription.EnsureCanUseExtraFields();
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
