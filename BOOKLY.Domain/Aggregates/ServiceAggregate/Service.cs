using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
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
        public int ServiceTypeId { get; private set; }
        public Duration DurationMinutes { get; private set; } = null!;
        public Mode Mode { get; private set; }
        public bool IsActive { get; private set; }
        public decimal? Price { get; private set; }

        public IReadOnlyCollection<ServiceSchedule> ServiceSchedules => _serviceSchedules.AsReadOnly();
        public IReadOnlyCollection<ServiceSecretary> ServiceSecretaries => _serviceSecretaries.AsReadOnly();
        public IReadOnlyCollection<int> SecretaryIds =>
            _serviceSecretaries.Select(x => x.SecretaryId).ToList().AsReadOnly();
        public IReadOnlyCollection<ServiceUnavailability> ServicesUnavailability => _serviceUnavailability.AsReadOnly();


        // Navegación 

        // EF core ctor
        private Service() { }
        public static Service Create(
            string name,
            int userId,
            string slug,
            string? description,
            int serviceType,
            Duration duration,
            Mode mode,
            decimal? price)
        {
            ValidateString(name, "nombre");

            if (userId <= 0)
                throw new DomainException("El dueño es requerido");

            return new Service
            {
                Name = name.Trim(),
                OwnerId = userId,
                Slug = Slug.Create(slug),
                Description = description?.Trim(),
                ServiceTypeId = serviceType,
                DurationMinutes = duration,
                Mode = mode,
                Price = price,
                IsActive = true
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

        public void ChangePrice(decimal price)
        {
            if(price < 0)
                throw new DomainException("El precio tiene que ser mayor a 0.");
            if(Price !=  price) 
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

            _serviceSecretaries.Clear();
            _serviceSecretaries.AddRange(secretaryIds.Select(ServiceSecretary.Create));
        }

        public void SetSchedules(List<ServiceSchedule> schedules)
        {
            if (schedules == null || !schedules.Any())
                throw new DomainException("Debe proporcionar al menos un horario");

            // Validar que horarios no se solapan
            ValidateNoOverlappingSchedules(schedules);

            _serviceSchedules.Clear();
            _serviceSchedules.AddRange(schedules);
        }
        private void ValidateNoOverlappingSchedules(List<ServiceSchedule> schedules)
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
                        throw new DomainException($"Schedule overlap detected on {dayGroup.Key}");
                }
            }
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

        //
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
