using AutoMapper;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Mappings;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Application.Services.ClientAggregate.DTOs;
using BOOKLY.Domain.Aggregates.UserAggregate;
using BOOKLY.Domain.Aggregates.UserAggregate.Enums;
using BOOKLY.Domain.Interfaces;

namespace BOOKLY.Application.Services.ClientAggregate
{
    public sealed class ClientService : IClientService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMapper _mapper;

        public ClientService(
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IUserRepository userRepository,
            IDateTimeProvider dateTimeProvider,
            IMapper mapper)
        {
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _userRepository = userRepository;
            _dateTimeProvider = dateTimeProvider;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyCollection<ClientListItemDto>>> GetByOwner(int ownerId, string? search = null, CancellationToken ct = default)
        {
            var appointmentsResult = await LoadAppointmentsByOwner(ownerId, ct);
            if (appointmentsResult.Error is not null)
                return Result<IReadOnlyCollection<ClientListItemDto>>.Failure(appointmentsResult.Error);

            var clients = appointmentsResult.Appointments!
                .GroupBy(a => a.ClientEmail, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var ordered = group.OrderBy(a => a.StartDateTime).ToList();
                    var now = _dateTimeProvider.NowArgentina();
                    var last = ordered.LastOrDefault(a => a.StartDateTime <= now);
                    var next = ordered.FirstOrDefault(a => a.StartDateTime > now);
                    var sample = ordered.Last();

                    return new ClientListItemDto
                    {
                        Name = sample.ClientName,
                        Email = sample.ClientEmail,
                        Phone = sample.ClientPhone,
                        TotalAppointments = ordered.Count,
                        LastAppointmentDateTime = last?.StartDateTime,
                        NextAppointmentDateTime = next?.StartDateTime
                    };
                });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                clients = clients.Where(client =>
                    client.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    client.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    client.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return Result<IReadOnlyCollection<ClientListItemDto>>.Success(
                clients.OrderBy(c => c.Name).ToList());
        }

        public async Task<Result<ClientDetailDto>> GetDetail(int ownerId, string email, CancellationToken ct = default)
        {
            var historyResult = await GetAppointmentHistory(ownerId, email, ct);
            if (historyResult.IsFailure)
                return Result<ClientDetailDto>.Failure(historyResult.Error!);

            var appointments = historyResult.Data!;
            if (!appointments.Any())
                return Result<ClientDetailDto>.Failure(Error.NotFound("Cliente"));

            var ordered = appointments.OrderBy(a => a.StartDateTime).ToList();
            var now = _dateTimeProvider.NowArgentina();
            var last = ordered.LastOrDefault(a => a.StartDateTime <= now);
            var next = ordered.FirstOrDefault(a => a.StartDateTime > now);
            var sample = ordered.Last();

            return Result<ClientDetailDto>.Success(new ClientDetailDto
            {
                Name = sample.ClientName,
                Email = sample.ClientEmail,
                Phone = sample.ClientPhone,
                TotalAppointments = ordered.Count,
                FirstAppointmentDateTime = ordered.First().StartDateTime,
                LastAppointmentDateTime = last?.StartDateTime,
                NextAppointmentDateTime = next?.StartDateTime,
                ServicesUsed = ordered
                    .Select(a => a.ServiceName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToList()
            });
        }

        public async Task<Result<IReadOnlyCollection<AppointmentListItemDto>>> GetAppointmentHistory(int ownerId, string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(Error.Validation("El email del cliente es requerido."));

            var appointmentsResult = await LoadAppointmentsByOwner(ownerId, ct);
            if (appointmentsResult.Error is not null)
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(appointmentsResult.Error);

            var result = appointmentsResult.Appointments!
                .Where(a => string.Equals(a.ClientEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.StartDateTime)
                .ToList();

            if (!result.Any())
                return Result<IReadOnlyCollection<AppointmentListItemDto>>.Failure(Error.NotFound("Cliente"));

            return Result<IReadOnlyCollection<AppointmentListItemDto>>.Success(result);
        }

        private async Task<(Error? Error, List<AppointmentListItemDto>? Appointments)> LoadAppointmentsByOwner(int ownerId, CancellationToken ct)
        {
            var owner = await _userRepository.GetOne(ownerId, ct);
            if (owner == null || owner.Role != UserRole.Owner)
                return (Error.NotFound("Usuario"), null);

            var services = await _serviceRepository.GetServicesByOwner(ownerId, ct);
            var appointments = new List<AppointmentListItemDto>();

            foreach (var service in services)
            {
                var serviceAppointments = await _appointmentRepository.GetByService(service.Id, ct);
                appointments.AddRange(serviceAppointments.Select(appointment => MapAppointmentListItem(appointment, service.Name)));
            }

            return (null, appointments);
        }

        private AppointmentListItemDto MapAppointmentListItem(
            Domain.Aggregates.AppointmentAggregate.Appointment appointment,
            string serviceName)
        {
            return _mapper.Map<AppointmentListItemDto>(appointment, options =>
            {
                options.Items[AppointmentMappingProfile.ServiceNameContextKey] = serviceName;
            });
        }
    }
}
