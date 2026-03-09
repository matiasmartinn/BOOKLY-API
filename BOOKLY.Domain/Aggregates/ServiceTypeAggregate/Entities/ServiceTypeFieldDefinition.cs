using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.ValueObjects;

namespace BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities
{
    /// <summary>
    /// PROPERTIES: Key, Label, Description?, FieldType, IsRequired, IsActive, SortOrder, options
    /// </summary>
    public class ServiceTypeFieldDefinition
    {
        public int Id { get; private set; }
        public int ServiceTypeId { get; private set; }

        public FieldKey Key { get; private set; } = null!;
        public FieldLabel Label { get; private set; } = null!;
        public string? Description { get; private set; }

        public ServiceFieldType FieldType { get; private set; }
        public bool IsRequired { get; private set; }
        public bool IsActive { get; private set; }
        public int SortOrder { get; private set; }

        public DateTime CreatedOn { get; private set; }
        public DateTime? UpdatedOn { get; private set; }

        private readonly List<ServiceTypeFieldOption> _options = new();
        public IReadOnlyCollection<ServiceTypeFieldOption> Options => _options.AsReadOnly();

        private ServiceTypeFieldDefinition() { }

        internal static ServiceTypeFieldDefinition Create(int serviceTypeId,
            FieldKey key,
            FieldLabel label,
            ServiceFieldType fieldType,
            bool isRequired,
            int sortOrder,
            string? description)
        {
            if (serviceTypeId <= 0)
                throw new DomainException("El ServiceTypeId es requerido.");

            if (sortOrder < 0)
                throw new DomainException("El orden no puede ser negativo.");

            return new ServiceTypeFieldDefinition
            {
                ServiceTypeId = serviceTypeId,
                Key = key,
                Label = label,
                FieldType = fieldType,
                IsRequired = isRequired,
                SortOrder = sortOrder,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                IsActive = true,
                CreatedOn = DateTime.Now
            };
        }

        public void ChangeLabel(FieldLabel label)
        {
            if (Label.Equals(label))
                return;

            Label = label;
        }

        public void ChangeDescription(string? desc)
        {
            Description = string.IsNullOrWhiteSpace(desc) ? null : desc.Trim();
        }
        public void ChangeFieldType(ServiceFieldType type)
        {
            if (FieldType == type) return;

            FieldType = type;

            if (FieldType != ServiceFieldType.Select)
                _options.Clear();
        }

        public void SetRequired(bool required)
        {
            if (IsRequired == required)
                return;

            IsRequired = required;
        }

        public void ChangeSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new DomainException("El orden del campo es requerido.");
            if (SortOrder == sortOrder)
                return;

            SortOrder = sortOrder;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;
            IsActive = false;
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
        }

        public void AddOption(string value, string label, int sortOrder)
        {
            EnsureSelect();

            ServiceTypeFieldOption.ValidateValue(value);
            ServiceTypeFieldOption.ValidateLabel(label);

            if (_options.Any(o => o.Value.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new DomainException("Ya existe una opción con ese value.");

            _options.Add(ServiceTypeFieldOption.Create(Id, value, label, sortOrder));

        }

        public void RemoveOption(int optionId)
        {
            EnsureSelect();

            var opt = _options.FirstOrDefault(o => o.Id == optionId);
            if (opt is null)
                throw new DomainException("La opción no existe.");

            _options.Remove(opt);
        }

        internal void UpdateOption(int optionId, string? label, int? sortOrder)
        {
            EnsureSelect();
            var opt = GetOptionOrThrow(optionId);
            if (label != null) opt.ChangeLabel(label);
            if (sortOrder.HasValue) opt.ChangeSortOrder(sortOrder.Value);
            UpdatedOn = DateTime.UtcNow;
        }

        internal void DeactivateOption(int optionId)
        {
            EnsureSelect();
            var opt = GetOptionOrThrow(optionId);
            opt.Deactivate();
        }

        internal void ActivateOption(int optionId)
        {
            EnsureSelect();
            var opt = GetOptionOrThrow(optionId);
            opt.Activate();
        }

        private ServiceTypeFieldOption GetOptionOrThrow(int optionId)
        {
            var opt = _options.FirstOrDefault(o => o.Id == optionId);
            if (opt is null)
                throw new DomainException("La opción no existe.");
            return opt;
        }
        public void EnsureUsable()
        {
            if (FieldType == ServiceFieldType.Select)
            {
                if (_options.Count == 0 || _options.All(o => !o.IsActive))
                    throw new DomainException("Un campo Select debe tener al menos una opción activa.");
            }
        }

        /// <summary>
        /// Valida que el campo sea un select.
        /// </summary>
        /// <exception cref="DomainException"></exception>
        private void EnsureSelect()
        {
            if (FieldType != ServiceFieldType.Select)
                throw new DomainException("Las opciones solo aplican a campos de tipo Select.");
        }
    }
}
