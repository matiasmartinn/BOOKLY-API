using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.SharedKernel;

namespace BOOKLY.Domain.Aggregates.AppointmentAggregate
{
    public sealed partial class Appointment : AggregateRoot
    {
        public int Id { get; private set; }
        public int ServiceId { get; private set; }
        public int? AssignedSecretaryId { get; private set; }

        // Client Data
        public ClientInfo Client { get; private set; } = null!;
        
        // Time Data
        public DateTime StartDateTime { get; private set; }
        public Duration Duration { get; private set; } = null!;
        public DateTime EndDateTime { get; private set; }
        //
        public AppointmentStatus Status { get; private set; }
        public string? ClientNotes { get; private set; }
        public string? CancelReason { get; private set; }
        public DateTime? CancelledOn { get; private set; }

        // Campos dinámicos
        private readonly List<AppointmentFieldValue> _fieldValues = new();
        public IReadOnlyCollection<AppointmentFieldValue> FieldValues => _fieldValues.AsReadOnly();

        // Audit
        public DateTime CreatedOn { get; private set; }
        public DateTime? UpdateOn { get; private set; }
        public int? UpdateBy { get; private set; }

        private Appointment() { }

        public static Appointment Create(
              int serviceId
            , int? secretaryId
            , ClientInfo client
            , DateTime startDateTime
            , Duration duration
            , string? clientNotes
            
            )
        {
            if (serviceId <= 0)
                throw new DomainException("El id tiene que ser un valor válido.");

            if (startDateTime <= DateTime.Now)
                throw new DomainException("El turno debe agendarse en un horario futuro.");

            var appointment = new Appointment
            {
                ServiceId = serviceId,
                AssignedSecretaryId = secretaryId,
                Client = client,
                StartDateTime = startDateTime,
                Duration = duration,
                EndDateTime = startDateTime.Add(TimeSpan.FromMinutes(duration.Value)),
                Status = AppointmentStatus.Pending,
                ClientNotes = clientNotes?.Trim(),
                CreatedOn = DateTime.Now
            };

            appointment.AddDomainEvent(new AppointmentCreatedEvent(
                appointment.Id,
                serviceId,
                client.ClientName,
                client.Email.Value,
                startDateTime,
                DateTime.Now));

            return appointment;
        }

        public static Appointment CreateHistorical(
              int serviceId
            , int? secretaryId
            , ClientInfo client
            , DateTime startDateTime
            , Duration duration
            , AppointmentStatus status
            , string? clientNotes
            , string? InternalNotes
            )
        {
            if (serviceId <= 0)
                throw new DomainException("El Servicio es requerido");

            var appointment = new Appointment
            {
                ServiceId = serviceId,
                AssignedSecretaryId = secretaryId,
                Client = client,
                StartDateTime = startDateTime,
                Duration = duration,
                EndDateTime = startDateTime.Add(TimeSpan.FromMinutes(duration.Value)),
                Status = status,
                ClientNotes = clientNotes?.Trim(),
                CreatedOn = DateTime.Now
            };

            appointment.AddDomainEvent(new AppointmentCreatedEvent(
                appointment.Id,
                serviceId,
                client.ClientName,
                client.Email.Value,
                startDateTime,
                DateTime.Now));

            return appointment;
        }
        public void AddFieldValue(int fieldDefinitionId, string value)
        {
            if (_fieldValues.Any(f => f.FieldDefinitionId == fieldDefinitionId))
                throw new DomainException("Ya existe un valor para este campo.");

            _fieldValues.Add(AppointmentFieldValue.Create(fieldDefinitionId, value));
        }

        public void ChangeClientInfo(ClientInfo newClient)
        {
            if (Client == newClient)
                return;

            Client = newClient;
        }

        public void Cancel(string? reason)
        {
            if (Status == AppointmentStatus.NoShow || Status == AppointmentStatus.Completed)
                throw new DomainException("No pueden cancelarse los turnos completados/ausentes");

            if (Status == AppointmentStatus.Cancelled)
                return;

            if(!string.IsNullOrEmpty(reason))
                CancelReason = reason?.Trim();
    
            var oldStatus = Status;
            Status = AppointmentStatus.Cancelled;
            CancelledOn = DateTime.Now;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.Cancelled, reason, DateTime.Now));
        }

        public void Confirm()
        {
            if (Status != AppointmentStatus.Pending)
                throw new DomainException("Solo los turnos pendientes pueden confirmarse.");

            var oldStatus = Status;
            Status = AppointmentStatus.Confirmed;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.Confirmed, null, DateTime.Now));
        }

        public void MarkAsCompleted()
        {
            if (Status != AppointmentStatus.Confirmed)
                throw new DomainException("Solo los turnos confirmados pueden completarse.");

            if (StartDateTime >= DateTime.Now)
                throw new DomainException("El turno debe haber iniciado.");

            var oldStatus = Status;
            Status = AppointmentStatus.Completed;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.Completed, null, DateTime.Now));
        }

        public void MarkAsNoShow()
        {
            if (Status != AppointmentStatus.Confirmed)
                throw new DomainException("Solo en los turnos confirmados pueden marcarse como ausentes.");

            if (StartDateTime >= DateTime.Now)
                throw new DomainException("El turno debe haber iniciado.");

            var oldStatus = Status;
            Status = AppointmentStatus.NoShow;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.NoShow, null, DateTime.Now));
        }

        public void ChangeInternalNotes(string notes)
        {
            CancelReason = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        }
    }    
}
