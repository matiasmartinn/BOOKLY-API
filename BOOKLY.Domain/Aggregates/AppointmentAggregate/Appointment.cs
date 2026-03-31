using BOOKLY.Domain.Aggregates.AppointmentAggregate.Entities;
using BOOKLY.Domain.Aggregates.AppointmentAggregate.Events;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Exceptions;
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
        public DateTime EndDateTime { get; private set; }
        public Duration Duration { get; private set; } = null!;
        
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
            , DateTime now
            , int? actorUserId = null
            )
        {
            if (serviceId <= 0)
                throw new DomainException("El id tiene que ser un valor válido.");

            if (startDateTime <= now)
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
                CreatedOn = now
            };

            appointment.AddDomainEvent(new AppointmentCreatedEvent(
                appointment,
                now,
                actorUserId));

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
            , DateTime now
            , int? actorUserId = null
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
                CreatedOn = now
            };

            appointment.AddDomainEvent(new AppointmentCreatedEvent(
                appointment,
                now,
                actorUserId));

            return appointment;
        }
        public void AddFieldValue(int fieldDefinitionId, string value)
        {
            if (_fieldValues.Any(f => f.FieldDefinitionId == fieldDefinitionId))
                throw new DomainException("Ya existe un valor para este campo.");

            _fieldValues.Add(AppointmentFieldValue.Create(fieldDefinitionId, value));
        }

        public void ChangeClientInfo(ClientInfo newClient, DateTime now)
        {
            if (Client == newClient)
                return;

            Client = newClient;
            UpdateOn = now;
        }

        public void ChangeClientNotes(string? notes, DateTime now)
        {
            var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (ClientNotes == normalizedNotes)
                return;

            ClientNotes = normalizedNotes;
            UpdateOn = now;
        }

        public void Reschedule(DateTime start, Duration duration, DateTime now)
        {
            if (Status != AppointmentStatus.Pending)
                throw new DomainException("Solo los turnos pendientes pueden reprogramarse.");

            if (start <= now)
                throw new DomainException("El turno debe reprogramarse en un horario futuro.");

            StartDateTime = start;
            Duration = duration;
            EndDateTime = start.Add(TimeSpan.FromMinutes(duration.Value));
            UpdateOn = now;
        }

        public void MarkAsCancel(string? reason, DateTime now, int? actorUserId = null)
        {
            if (Status == AppointmentStatus.NoShow || Status == AppointmentStatus.Attended)
                throw new DomainException("No pueden cancelarse los turnos completados/ausentes");

            if (Status == AppointmentStatus.Cancelled)
                return;

            if(!string.IsNullOrEmpty(reason))
                CancelReason = reason?.Trim();
    
            var oldStatus = Status;
            Status = AppointmentStatus.Cancelled;
            CancelledOn = now;
            UpdateOn = now;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.Cancelled, reason, now, actorUserId));
        }

        public void MarkAsAttended(DateTime now, int? actorUserId = null)
        {
            if (Status != AppointmentStatus.Pending)
                throw new DomainException("Solo los turnos pendientes pueden completarse.");

            if (StartDateTime >= now)
                throw new DomainException("El turno debe haber iniciado.");

            var oldStatus = Status;
            Status = AppointmentStatus.Attended;
            UpdateOn = now;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.Attended, null, now, actorUserId));
        }

        public void MarkAsNoShow(DateTime now, int? actorUserId = null)
        {
            if (Status != AppointmentStatus.Pending)
                throw new DomainException("Solo los turnos pendientes pueden marcarse como ausentes.");

            if (StartDateTime >= now)
                throw new DomainException("El turno debe haber iniciado.");

            var oldStatus = Status;
            Status = AppointmentStatus.NoShow;
            UpdateOn = now;

            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, oldStatus, AppointmentStatus.NoShow, null, now, actorUserId));
        }

    }    
}
