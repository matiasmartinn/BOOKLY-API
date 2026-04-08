using System.Net;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Enums;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate
{
    public sealed class Service
    {
        // Campos privados
        private readonly List<ServiceSecretary> _serviceSecretaries = new();
        private readonly List<ServiceSchedule> _serviceSchedules = new();
        private readonly List<ServiceUnavailability> _serviceUnavailability = new();

        // Propiedades del agregado
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;
        public int OwnerId { get; private set; }
        public Slug Slug { get; private set; } = null!;
        public string? Description { get; private set; }
        public Location? Location { get; private set; }
        public int ServiceTypeId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Duration DurationMinutes { get; private set; } = null!;
        public Capacity Capacity { get; private set; } = null!;
        public Mode Mode { get; private set; }
        public bool IsActive { get; private set; }
        public decimal? Price { get; private set; }

        public bool IsPublicBookingEnabled { get; private set; } = true;
        public string PublicBookingToken { get; private set; } = null!;
        public DateTime? PublicBookingTokenUpdateAt { get; private set; }


        public IReadOnlyCollection<ServiceSchedule> ServiceSchedules => _serviceSchedules.AsReadOnly();
        public IReadOnlyCollection<ServiceSecretary> ServiceSecretaries => _serviceSecretaries.AsReadOnly();
        public IReadOnlyCollection<int> SecretaryIds =>
            _serviceSecretaries.Select(x => x.SecretaryId).ToList().AsReadOnly();
        public IReadOnlyCollection<ServiceUnavailability> ServicesUnavailability => _serviceUnavailability.AsReadOnly();

        // EF core ctor
        private Service() { }
        public static Service Create(
            string name,
            int userId,
            string slug,
            string? description,
            Location? location,
            int serviceType,
            DateTime createdAt,
            Duration duration,
            Capacity capacity,
            Mode mode,
            decimal? price)
        {
            ValidateString(name, "nombre");

            if (userId <= 0)
                throw new DomainException("El dueño es requerido");

            if (createdAt == default)
                throw new DomainException("La fecha de creaciÃ³n es requerida");

            return new Service
            {
                Name = name.Trim(),
                OwnerId = userId,
                Slug = Slug.Create(slug),
                Description = description?.Trim(),
                Location = location,
                ServiceTypeId = serviceType,
                CreatedAt = createdAt,
                DurationMinutes = duration,
                Capacity = capacity,
                Mode = mode,
                Price = price,
                IsActive = true,
                IsPublicBookingEnabled = true,
                PublicBookingToken = GeneratePublicBookingToken(),
                PublicBookingTokenUpdateAt = createdAt
            };
        }

        public void ChangeName(string name)
        {
            ValidateString(name, "nombre");
            if (Name != name.Trim())
                Name = name.Trim();
        }

        public void ChangeDescription(string description)
        {
            ValidateString(description, "descripcion");
            Description = description.Trim();
        }

        public void ChangeLocation(string? placeName, string? address, string? googleMapsUrl)
        {
            Location = Location.Create(placeName, address, googleMapsUrl);
        }

        public void ChangeSlug(string slug)
        {
            var newSlug = Slug.Create(slug);
            if (!newSlug.Equals(Slug))
                Slug = newSlug;
        }

        public void ChangeDuration(int duration)
        {
            var newDuration = Duration.Create(duration);
            DurationMinutes = newDuration;
        }

        public void ChangeCapacity(int capacity)
        {
            Capacity = ValueObjects.Capacity.Create(capacity);
        }

        public void ChangePrice(decimal price)
        {
            if (price < 0)
                throw new DomainException("El precio tiene que ser mayor a 0.");
            if (Price != price)
                Price = price;
        }
        public void ChangeServiceType(int serviceTypeId)
        {
            if (serviceTypeId <= 0)
                throw new DomainException("EL tipo de servicio es requerido.");

            ServiceTypeId = serviceTypeId;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }
        // ========== SECRETARIOS ==========
        public void AssignSecretary(int secretaryId)
        {
            if (secretaryId <= 0) throw new DomainException("ID de secretario inválido");
            if (_serviceSecretaries.Any(x => x.SecretaryId == secretaryId))
                throw new DomainException("El secretario ya está asignado a este servicio");

            _serviceSecretaries.Add(ServiceSecretary.Create(secretaryId));
        }

        public void RemoveSecretary(int secretaryId)
        {
            _serviceSecretaries.RemoveAll(x => x.SecretaryId == secretaryId);
        }

        public void AssignSecretaries(List<int> secretaryIds)
        {
            if (secretaryIds == null || secretaryIds.Count == 0)
            {
                _serviceSecretaries.Clear();
                return;
            }

            if (secretaryIds.Distinct().Count() != secretaryIds.Count)
                throw new DomainException("La lista contiene IDs duplicados");

            if (secretaryIds.Any(id => id <= 0))
                throw new DomainException("Todos los IDs deben ser mayores a 0");

            var existingSecretaries = _serviceSecretaries.ToDictionary(x => x.SecretaryId);
            var updatedSecretaries = secretaryIds
                .Select(secretaryId =>
                    existingSecretaries.TryGetValue(secretaryId, out var existingSecretary)
                        ? existingSecretary
                        : ServiceSecretary.Create(secretaryId))
                .ToList();

            _serviceSecretaries.Clear();
            _serviceSecretaries.AddRange(updatedSecretaries);
        }

        public void GrantSecretaryPermission(int secretaryId, SecretaryPermission permission)
        {
            var secretary = GetRequiredSecretary(secretaryId);
            secretary.GrantPermission(permission);
        }

        public void RevokeSecretaryPermission(int secretaryId, SecretaryPermission permission)
        {
            var secretary = GetRequiredSecretary(secretaryId);
            secretary.RevokePermission(permission);
        }

        public bool SecretaryHasPermission(int secretaryId, SecretaryPermission permission)
        {
            var secretary = GetRequiredSecretary(secretaryId);
            return secretary.HasPermission(permission);
        }

        public void SetSchedules(IEnumerable<ServiceSchedule> schedules)
        {
            var schedulesList = schedules?.ToList()
                ?? throw new DomainException("Debe proporcionar al menos un horario");

            if (!schedulesList.Any())
                throw new DomainException("Debe proporcionar al menos un horario");

            // Validar que horarios no se solapan
            ValidateNoOverlappingSchedules(schedulesList);

            _serviceSchedules.Clear();
            _serviceSchedules.AddRange(schedulesList);
        }
        private void ValidateNoOverlappingSchedules(IEnumerable<ServiceSchedule> schedules)
        {
            // Agrupar por dia
            var schedulesByDay = schedules.GroupBy(s => s.Day.Value);

            foreach (var dayGroup in schedulesByDay)
            {
                var orderedSchedules = dayGroup.OrderBy(s => s.Range.Start).ToList();

                for (int i = 0; i < orderedSchedules.Count - 1; i++)
                {
                    var current = orderedSchedules[i];
                    var next = orderedSchedules[i + 1];

                    if (current.Range.End > next.Range.Start)
                        throw new DomainException($"Existen horarios solapados para el día {dayGroup.Key}.");
                }
            }
        }

        public ServiceSchedule? GetScheduleFor(DateTime startDateTime, Duration duration)
        {
            return _serviceSchedules
                .OrderBy(s => s.Range.Start)
                .FirstOrDefault(s => s.CanHost(startDateTime, duration));
        }


        public bool IsFullDayBlocked(DateOnly date)
        {
            return _serviceUnavailability.Any(u => u.BlocksWholeDay(date));
        }

        public IReadOnlyCollection<TimeRange> GetBlockedRanges(DateOnly date)
        {
            return _serviceUnavailability
                .Where(u => u.AppliesTo(date) && u.TimeRange is not null)
                .Select(u => u.TimeRange!)
                .ToList()
                .AsReadOnly();
        }

        public void AddUnavailability(DateRange dateRange, TimeRange? timeRange, string? reason)
        {
            var newUnavailability = ServiceUnavailability.Create(dateRange, timeRange, reason);

            if (_serviceUnavailability.Any(x => x.OverlapsWith(newUnavailability)))
                throw new DomainException("Ya existe una inhabilitación que se superpone con el rango indicado.");

            _serviceUnavailability.Add(newUnavailability);
        }

        public void RemoveUnavailability(int unavailabilityId)
        {
            var item = _serviceUnavailability.FirstOrDefault(x => x.Id == unavailabilityId);
            if (item is null)
                throw new DomainException("La inhabilitación no existe.");

            _serviceUnavailability.Remove(item);
        }

        public void EnablePublicBooking(DateTime now)
        {
            IsPublicBookingEnabled = true;

            if (string.IsNullOrWhiteSpace(PublicBookingToken))
            {
                PublicBookingToken = GeneratePublicBookingToken();
                PublicBookingTokenUpdateAt = now;
            }
        }

        public void DisablePublicBooking()
        {
            IsPublicBookingEnabled = false;
        }

        public void RegeneratePublicBookingToken(DateTime now)
        {
            PublicBookingToken = GeneratePublicBookingToken();
            PublicBookingTokenUpdateAt = now;
        }

        public bool MatchesPublicBookingAccess(string slug, string token)
        {
            return string.Equals(Slug.Value, NormalizeSlug(slug), StringComparison.Ordinal) &&
                   string.Equals(PublicBookingToken, NormalizePublicBookingToken(token), StringComparison.Ordinal);
        }

        public bool HasValidPublicBookingAccess(string slug, string token)
        {
            return MatchesPublicBookingAccess(slug, token) &&
                   IsActive &&
                   IsPublicBookingEnabled;
        }

        public void EnabledPublicBooking(DateTime now) => EnablePublicBooking(now);

        public void DisabledPublicBooking() => DisablePublicBooking();

        private static string GeneratePublicBookingToken()
        {
            return Convert.ToHexString(Guid.NewGuid().ToByteArray())
                .Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static string NormalizeSlug(string slug)
        {
            return string.IsNullOrWhiteSpace(slug)
                ? string.Empty
                : slug.Trim().ToLowerInvariant().Replace(" ", "-");
        }

        private static string NormalizePublicBookingToken(string token)
        {
            return string.IsNullOrWhiteSpace(token)
                ? string.Empty
                : token.Trim().ToLowerInvariant();
        }

        private ServiceSecretary GetRequiredSecretary(int secretaryId)
        {
            if (secretaryId <= 0)
                throw new DomainException("ID de secretario inválido");

            var secretary = _serviceSecretaries.FirstOrDefault(x => x.SecretaryId == secretaryId);
            if (secretary == null)
                throw new DomainException("El secretario no pertenece a este servicio.");

            return secretary;
        }

        private static void ValidateString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException($"El {fieldName} es requerido.");

            var trimmed = value.Trim();

            if (trimmed.Length < 2)
                throw new DomainException($"El {fieldName} debe tener al menos 2 caracteres.");

            if (trimmed.Length > 100)
                throw new DomainException($"El {fieldName} no puede exceder los 100 caracteres.");
        }
    }
}
