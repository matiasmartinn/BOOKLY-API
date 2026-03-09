namespace BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities
{
    public sealed class ServiceTypeFieldOption
    {
        public int Id { get; private set; }
        public int FieldDefinitionId { get; private set; }
        public string Value { get; private set; } = null!;
        public string Label { get; private set; } = null!;
        public int SortOrder { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime? UpdatedOn { get; private set; }

        private ServiceTypeFieldOption() { }

        internal static ServiceTypeFieldOption Create(int fieldDefinitionId, string value, string label, int sortOrder)
        {
            if (fieldDefinitionId <= 0)
                throw new DomainException("El FieldDefinitionId es requerido.");

            ValidateValue(value);
            ValidateLabel(label);

            if (sortOrder < 0)
                throw new DomainException("El orden no puede ser negativo.");

            return new ServiceTypeFieldOption
            {
                FieldDefinitionId = fieldDefinitionId,
                Value = value.Trim(),
                Label = label.Trim(),
                SortOrder = sortOrder,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };
        }

        internal void ChangeLabel(string label)
        {
            ValidateLabel(label);
            var trimmed = label.Trim();
            if (Label == trimmed) return;
            Label = trimmed;
            UpdatedOn = DateTime.UtcNow;
        }

        internal void ChangeSortOrder(int sortOrder)
        {
            if (sortOrder < 0)
                throw new DomainException("El orden no puede ser negativo.");
            if (SortOrder == sortOrder) return;
            SortOrder = sortOrder;
            UpdatedOn = DateTime.UtcNow;
        }

        internal void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            UpdatedOn = DateTime.UtcNow;
        }

        internal void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            UpdatedOn = DateTime.UtcNow;
        }

        internal static void ValidateValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("El value de la opción es requerido.");
            if (value.Trim().Length > 60)
                throw new DomainException("El value de la opción no puede exceder 60 caracteres.");
        }

        internal static void ValidateLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new DomainException("El label de la opción es requerido.");
            if (label.Trim().Length > 80)
                throw new DomainException("El label de la opción no puede exceder 80 caracteres.");
        }
    }
}