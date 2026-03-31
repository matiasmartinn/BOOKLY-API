using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceTypeAggregate
{
    public sealed class ServiceType
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public bool IsActive { get; private set; }
        private readonly List<ServiceTypeFieldDefinition> _fieldDefinitions = new();
        public IReadOnlyCollection<ServiceTypeFieldDefinition> FieldDefinitions => _fieldDefinitions.AsReadOnly();

        private ServiceType() { }

        public static ServiceType Create(string name, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("El nombre del tipo de servicio es requerido");

            return new ServiceType
            {
                Name = name.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                IsActive = true
            };
        }

        // ========================
        // ServiceType behavior
        // ========================
        public void ChangeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("El nombre es requerido.");
            Name = name.Trim();
        }

        public void ChangeDescription(string? desc)
        {
            Description = desc?.Trim();
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;

        public bool HasActiveFields()
            => _fieldDefinitions.Any(field => field.IsActive);

        // ========================
        // Field behavior
        // ========================
        public ServiceTypeFieldDefinition AddField(
            string key, string label, ServiceFieldType type,
            bool isRequired, int sortOrder, DateTime now, string? description = null)
        {
            var fk = FieldKey.Create(key);       // VO creado acá, validación incluida
            var fl = FieldLabel.Create(label);

            if (_fieldDefinitions.Any(f => f.Key.Value.Equals(fk.Value, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException("Ya existe un campo con esa key para este tipo de servicio.");

            var def = ServiceTypeFieldDefinition.Create(Id, fk, fl, type, isRequired, sortOrder, description, now);
            _fieldDefinitions.Add(def);
            return def;
        }

        public void UpdateField(int fieldDefinitionId, string? label, string? description, bool? isRequired, int? sortOrder)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            if (label != null) def.ChangeLabel(FieldLabel.Create(label));
            if (description != null) def.ChangeDescription(description);
            if (isRequired.HasValue) def.SetRequired(isRequired.Value);
            if (sortOrder.HasValue) def.ChangeSortOrder(sortOrder.Value);
        }

        public void ChangeFieldType(int fieldDefinitionId, ServiceFieldType type)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.ChangeFieldType(type);
        }

        public void RemoveField(int fieldDefinitionId)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.Deactivate();
        }

        public void ActivateField(int fieldDefinitionId)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.EnsureUsable();
            def.Activate();
        }

        // ========================
        // Option behavior
        // ========================
        public void AddOptionToField(int fieldDefinitionId, string value, string label, int sortOrder, DateTime now)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.AddOption(value, label, sortOrder, now);
        }

        public void RemoveOptionFromField(int fieldDefinitionId, int optionId)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.RemoveOption(optionId);
        }

        public void DeactivateOption(int fieldDefinitionId, int optionId, DateTime now)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.DeactivateOption(optionId, now);
        }

        public void ActivateOption(int fieldDefinitionId, int optionId, DateTime now)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.ActivateOption(optionId, now);
        }

        public void UpdateOption(int fieldDefinitionId, int optionId, string? label, int? sortOrder, DateTime now)
        {
            var def = GetFieldOrThrow(fieldDefinitionId);
            def.UpdateOption(optionId, label, sortOrder, now);
        }

        // ========================
        // Private helpers
        // ========================
        private ServiceTypeFieldDefinition GetFieldOrThrow(int fieldDefinitionId)
        {
            var def = _fieldDefinitions.FirstOrDefault(x => x.Id == fieldDefinitionId);
            if (def is null)
                throw new DomainException("El campo no existe.");
            return def;
        }
    }
}
