using AutoMapper;
using BOOKLY.Application.Common;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Interfaces;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.AppointmentAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

namespace BOOKLY.Application.Services.AppointmentAggregate
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        public AppointmentService(
            IAppointmentRepository repository
            , IServiceRepository serviceRepository
            , IMapper mapper
            , IUnitOfWork unitOfWork
            )
        {
            _repository = repository;
            _serviceRepository = serviceRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<IReadOnlyCollection<AppointmentDto>>> GetByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
        
            if (service == null)
                    return Result<IReadOnlyCollection<AppointmentDto>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _repository.GetByService(serviceId, ct);
            return Result<IReadOnlyCollection<AppointmentDto>>.Success(_mapper.Map<IReadOnlyCollection<AppointmentDto>>(appointments));

        }

        public async Task<Result<IReadOnlyCollection<AppointmentSummaryDto>>> GetByServiceAndDate(int serviceId,DateOnly date, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IReadOnlyCollection<AppointmentSummaryDto>>.Failure(Error.NotFound("Servicio"));

            var appointments = await _repository.GetByServiceAndDate(serviceId, date, ct);
            return Result<IReadOnlyCollection<AppointmentSummaryDto>>.Success(_mapper.Map<IReadOnlyCollection<AppointmentSummaryDto>>(appointments));
        }
        public async Task<Result<AppointmentDto>> CreateAppointment(CreateAppointmentDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(dto.ServiceId, ct);
            if (service == null)
                return Result<AppointmentDto>.Failure(Error.NotFound("Servicio"));

            var endDateTime = dto.StartDateTime.Add(TimeSpan.FromMinutes(service.DurationMinutes.Value));

            var isBlocked = await _serviceRepository.ExistsBlock(service.Id, dto.StartDateTime, endDateTime, ct);
            if (isBlocked)
                return Result<AppointmentDto>.Failure(Error.Conflict("El horario se encuentra bloqueado."));

            var hasOverlap = await _repository.ExistsOverlap(dto.ServiceId, dto.StartDateTime, endDateTime, ct);
            if (hasOverlap)
                return Result<AppointmentDto>.Failure(Error.Conflict("El horario seleccionado ya no está disponible."));

                var email = Email.Create(dto.ClientEmail);
                var client = ClientInfo.Create(dto.ClientName, dto.ClientPhone, email);

                var appointment = Appointment.Create(
                    dto.ServiceId
                    , dto.AssignedSecretaryId
                    , client
                    , dto.StartDateTime
                    , service.DurationMinutes
                    , dto.ClientNotes
                );

                foreach (var fv in dto.FieldValues)
                    appointment.AddFieldValue(fv.FieldDefinitionId, fv.Value);

                await _repository.AddOne(appointment, ct);
                await _unitOfWork.SaveChanges(ct);
                return Result<AppointmentDto>.Success(_mapper.Map<AppointmentDto>(appointment));

        }

        // ========== ESTADO ==========

        public async Task<Result> Cancel(int id, CancelAppointmentDto dto, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.Cancel(dto.Reason);
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result> Confirm(int id, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.Confirm();
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result> MarkAsCompleted(int id, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.MarkAsCompleted();
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result> MarkAsNoShow(int id, CancellationToken ct = default)
        {
            var appointment = await _repository.GetOne(id, ct);
            if (appointment == null)
                return Result.Failure(Error.NotFound("Turno"));

            appointment.MarkAsNoShow();
            _repository.Update(appointment);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }
    }
}
