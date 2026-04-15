using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.PublicBooking.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.DomainServices;

namespace BOOKLY.Application.Services.PublicBooking
{
    public sealed class PublicBookingService : IPublicBookingService
    {
        private const int DefaultAvailabilityWindowInDays = 30;
        private static readonly Error InvalidPublicAccessError =
            new(ErrorType.NotFound, "Acceso publico invalido o inexistente.");

        private readonly IServiceRepository _serviceRepository;
        private readonly IServiceTypeRepository _serviceTypeRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAppointmentService _appointmentService;
        private readonly IAvailabilityService _availabilityService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public PublicBookingService(
            IServiceRepository serviceRepository,
            IServiceTypeRepository serviceTypeRepository,
            IUserRepository userRepository,
            IAppointmentRepository appointmentRepository,
            IAppointmentService appointmentService,
            IAvailabilityService availabilityService,
            IDateTimeProvider dateTimeProvider)
        {
            _serviceRepository = serviceRepository;
            _serviceTypeRepository = serviceTypeRepository;
            _userRepository = userRepository;
            _appointmentRepository = appointmentRepository;
            _appointmentService = appointmentService;
            _availabilityService = availabilityService;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result<PublicServiceBookingDto>> GetService(string slug, string code, CancellationToken ct = default)
        {
            var serviceResult = await ResolvePublicService(slug, code, ct);
            if (serviceResult.IsFailure)
                return Result<PublicServiceBookingDto>.Failure(serviceResult.Error);

            var service = serviceResult.Data!;
            var serviceType = await _serviceTypeRepository.GetByIdWithFields(service.ServiceTypeId, ct);
            if (serviceType == null)
                return Result<PublicServiceBookingDto>.Failure(Error.NotFound("TipoServicio"));

            var owner = await _userRepository.GetOne(service.OwnerId, ct);

            return Result<PublicServiceBookingDto>.Success(MapPublicService(service, serviceType, owner));
        }

        public async Task<Result<List<DateOnly>>> GetAvailableDates(
            string slug,
            string code,
            DateOnly? from,
            DateOnly? to,
            CancellationToken ct = default)
        {
            var serviceResult = await ResolvePublicServiceWithAvailability(slug, code, ct);
            if (serviceResult.IsFailure)
                return Result<List<DateOnly>>.Failure(serviceResult.Error);

            var now = _dateTimeProvider.NowArgentina();
            var effectiveFrom = from ?? DateOnly.FromDateTime(now);
            var effectiveTo = to ?? effectiveFrom.AddDays(DefaultAvailabilityWindowInDays);

            if (effectiveFrom > effectiveTo)
                return Result<List<DateOnly>>.Failure(Error.Validation("El rango de fechas es invalido."));

            var service = serviceResult.Data!;
            var appointments = await _appointmentRepository.GetByServiceAndDateRange(service.Id, effectiveFrom, effectiveTo, ct);
            var dates = _availabilityService.GetAvailableDates(service, appointments, effectiveFrom, effectiveTo, now);

            return Result<List<DateOnly>>.Success(dates.ToList());
        }

        public async Task<Result<List<DateTime>>> GetAvailableSlots(
            string slug,
            string code,
            DateOnly date,
            CancellationToken ct = default)
        {
            var serviceResult = await ResolvePublicServiceWithAvailability(slug, code, ct);
            if (serviceResult.IsFailure)
                return Result<List<DateTime>>.Failure(serviceResult.Error);

            var service = serviceResult.Data!;
            var appointments = await _appointmentRepository.GetByServiceAndDate(service.Id, date, ct);
            var slots = _availabilityService.GetAvailableSlots(service, appointments, date, _dateTimeProvider.NowArgentina());

            return Result<List<DateTime>>.Success(slots.ToList());
        }

        public async Task<Result<AppointmentDto>> CreateAppointment(
            string slug,
            string code,
            PublicCreateAppointmentDto dto,
            CancellationToken ct = default)
        {
            var serviceResult = await ResolvePublicService(slug, code, ct);
            if (serviceResult.IsFailure)
                return Result<AppointmentDto>.Failure(serviceResult.Error);

            var service = serviceResult.Data!;

            var createAppointmentDto = new CreateAppointmentDto
            {
                ServiceId = service.Id,
                ClientName = dto.ClientName,
                ClientPhone = dto.ClientPhone,
                ClientEmail = dto.ClientEmail,
                StartDateTime = dto.StartDateTime,
                ClientNotes = dto.ClientNotes,
                FieldValues = dto.FieldValues
                    .Select(field => new CreateAppointmentFieldValueDto
                    {
                        FieldDefinitionId = field.FieldDefinitionId,
                        Value = field.Value
                    })
                    .ToList()
            };

            return await _appointmentService.CreateAppointment(createAppointmentDto, ct);
        }

        private async Task<Result<Service>> ResolvePublicService(string slug, string code, CancellationToken ct)
        {
            var service = await _serviceRepository.GetBySlugAndPublicBookingCode(slug, code, ct);
            return ValidatePublicAccess(service);
        }

        private async Task<Result<Service>> ResolvePublicServiceWithAvailability(string slug, string code, CancellationToken ct)
        {
            var service = await _serviceRepository.GetBySlugAndPublicBookingCodeWithSchedulesAndUnavailability(slug, code, ct);
            return ValidatePublicAccess(service);
        }

        private static Result<Service> ValidatePublicAccess(Service? service)
        {
            if (service == null)
                return Result<Service>.Failure(InvalidPublicAccessError);

            if (!service.IsActive)
                return Result<Service>.Failure(Error.Conflict("El servicio compartido no se encuentra activo."));

            if (!service.IsPublicBookingEnabled)
                return Result<Service>.Failure(Error.Conflict("El acceso publico para este servicio esta deshabilitado."));

            return Result<Service>.Success(service);
        }

        private static PublicServiceBookingDto MapPublicService(
            Service service,
            ServiceType serviceType,
            BOOKLY.Domain.Aggregates.UserAggregate.User? owner)
        {
            return new PublicServiceBookingDto
            {
                ServiceId = service.Id,
                Slug = service.Slug.Value,
                Name = service.Name,
                OwnerName = owner is null
                    ? null
                    : $"{owner.PersonName.FirstName} {owner.PersonName.LastName}".Trim(),
                Description = service.Description,
                PhoneNumber = service.PhoneNumber,
                PlaceName = service.Location?.PlaceName,
                Address = service.Location?.Address,
                GoogleMapsUrl = service.Location?.GoogleMapsUrl,
                ServiceTypeId = service.ServiceTypeId,
                DurationMinutes = service.DurationMinutes.Value,
                Capacity = service.Capacity.Value,
                Mode = service.Mode.ToString(),
                Price = service.Price,
                FieldDefinitions = serviceType.FieldDefinitions
                    .Where(field => field.IsActive)
                    .OrderBy(field => field.SortOrder)
                    .ThenBy(field => field.Id)
                    .Select(field => new PublicServiceFieldDto
                    {
                        Id = field.Id,
                        Key = field.Key.Value,
                        Label = field.Label.Value,
                        Description = field.Description,
                        FieldType = (int)field.FieldType,
                        IsRequired = field.IsRequired,
                        SortOrder = field.SortOrder,
                        Options = field.Options
                            .Where(option => option.IsActive)
                            .OrderBy(option => option.SortOrder)
                            .ThenBy(option => option.Id)
                            .Select(option => new PublicServiceFieldOptionDto
                            {
                                Id = option.Id,
                                Value = option.Value,
                                Label = option.Label,
                                SortOrder = option.SortOrder
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}
