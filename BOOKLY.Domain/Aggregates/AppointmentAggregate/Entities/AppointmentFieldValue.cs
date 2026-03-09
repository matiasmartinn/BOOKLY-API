namespace BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities
{
    public sealed class AppointmentFieldValue
    {
        public int Id { get; private set; }
        public int AppointmentId { get; private set; }
        public int FieldDefinitionId { get; private set; }
        public string Value { get; private set; } = null!;
        private AppointmentFieldValue() { }
        internal AppointmentFieldValue(int fieldDefinitionId, string value)
        {
            FieldDefinitionId = fieldDefinitionId;
            Value = value;
        }
        public static AppointmentFieldValue Create(int fieldDefinitionId, string value)
        {
            if (fieldDefinitionId <= 0)
                throw new DomainException("FieldDefinitionId inválido.");

            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("El valor del campo no puede estar vacío.");

            return new AppointmentFieldValue(fieldDefinitionId, value.Trim());
        }
    }
}
