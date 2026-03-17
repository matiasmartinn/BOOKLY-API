using BOOKLY.Application.Services.ServiceAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceAggregate;
using BOOKLY.Domain.Interfaces;
using BOOKLY.Application.Common.Models;
using AutoMapper;
using BOOKLY.Application.Interfaces;
using BOOKLY.Domain.SharedKernel;
using BOOKLY.Application.Common;
using Microsoft.Extensions.Logging;
using BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects;
using BOOKLY.Domain.Aggregates.ServiceAggregate.Entities;
using BOOKLY.Domain.DomainServices;

namespace BOOKLY.Application.Services.ServiceAggregate
{
    public class ServiceApplicationService : IServiceApplicationService
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public ServiceApplicationService(
            IServiceRepository serviceRepository,
            IAppointmentRepository appointmentRepository,
            IAvailabilityService availabilityService,
            IUserRepository userRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork
            ) 
        {
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _availabilityService = availabilityService;
            _userRepository = userRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<ServiceDto>> GetServiceById(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
        }

        public async Task<Result<List<ServiceDto>>> GetServicesByOwner(int ownerId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetOne(ownerId, ct);
            if (user == null)
                return Result<List<ServiceDto>>.Failure(Error.NotFound("Usuario"));

            var services = await _serviceRepository.GetServicesByOwner(ownerId, ct);
            if (!services.Any())
                return Result<List<ServiceDto>>.Failure(Error.NotFound("Services"));

            return Result<List<ServiceDto>>.Success(_mapper.Map<List<ServiceDto>>(services));
        }
        public async Task<Result<ServiceDto>> CreateService(CreateServiceDto dto, CancellationToken ct = default)
        {
            var service = Service.Create(
                dto.Name,
                dto.OwnerId,
                dto.Slug,
                dto.Description,
                dto.ServiceTypeId,
                Duration.Create(dto.DurationMinutes),
                Mode.Presence,
                dto.Price
            );

            var schedules = dto.Schedules
                .Select(s =>
                    ServiceSchedule.Create(
                        TimeRange.Create(s.StartTime, s.EndTime),
                        Capacity.Create(s.Capacity),
                        Day.Create(s.Day)
                    )
                ).ToList();

            service.SetSchedules(schedules);

            await _serviceRepository.AddOne(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
        }

        public async Task<Result<ServiceDto>> UpdateService(int id, UpdateServiceDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);

            if(service == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            if(dto.Name != null) {
                service.ChangeName(dto.Name);
            }
            if(dto.Slug != null) {
                service.ChangeSlug(dto.Slug);
            }
            if(dto.Description != null) {
                service.ChangeDescription(dto.Description);
            }
            if(dto.DurationMinutes != null ) {
                service.ChangeDuration(dto.DurationMinutes.Value);
            }
            if(dto.Price != null){
                service.ChangePrice(dto.Price.Value);
            }
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(service));
        }

        public async Task<Result> DeleteService(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);

            if (service == null)
            return Result.Failure(Error.NotFound("Servicio"));
            
            _serviceRepository.Remove(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<ServiceScheduleDto>>> GetSchedulesByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IEnumerable<ServiceScheduleDto>>.Failure(Error.NotFound("Servicio"));

            var schedules = await _serviceRepository.GetSchedulesByService(serviceId, ct);
            
            return Result<IEnumerable<ServiceScheduleDto>>.Success(_mapper.Map<IEnumerable<ServiceScheduleDto>>(schedules));
        }

        public async Task<Result<IEnumerable<ScheduleUnavailabilityDto>>> GetUnavailabilityByService(int serviceId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(serviceId, ct);
            if (service == null)
                return Result<IEnumerable<ScheduleUnavailabilityDto>>.Failure(Error.NotFound("Servicio"));

            var unavailability = await _serviceRepository.GetUnavailabilityByService(serviceId, ct);

            return Result<IEnumerable<ScheduleUnavailabilityDto>>.Success(_mapper.Map<IEnumerable<ScheduleUnavailabilityDto>>(unavailability));
        }

        public async Task<Result<ServiceDto>> SetSchedule(int id, List<CreateServiceScheduleDto> dto, CancellationToken ct = default)
        {
            var services = await _serviceRepository.GetOneWithSchedules(id, ct);

            if (services == null)
                return Result<ServiceDto>.Failure(Error.NotFound("Servicio"));

            var schedules = dto
                .Select(s =>
                    ServiceSchedule.Create(
                        TimeRange.Create(s.StartTime, s.EndTime),
                        Capacity.Create(s.Capacity),
                        Day.Create(s.Day)
                    )
                ).ToList();
            services.SetSchedules(schedules);
            
            _serviceRepository.Update(services);
            await _unitOfWork.SaveChanges(ct);
            return Result<ServiceDto>.Success(_mapper.Map<ServiceDto>(services));
        }

        // ========== UNAVAILABILITIES ==========

        public async Task<Result<List<ScheduleUnavailabilityDto>>> GetScheduleUnavailability(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result<List<ScheduleUnavailabilityDto>>.Failure(Error.NotFound("Servicio"));

            var dto = _mapper.Map<List<ScheduleUnavailabilityDto>>(service.ServicesUnavailability);
            return Result<List<ScheduleUnavailabilityDto>>.Success(dto);
        }
        public async Task<Result> AddUnavailability(int id, CreateUnavailabilityDto dto, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            var hasOnlyOneTime = dto.StartTime.HasValue != dto.EndTime.HasValue;
            if (hasOnlyOneTime)
                return Result.Failure(Error.Validation("Debe indicar hora de inicio y fin, o ninguna."));

            var dateRange = DateRange.Create(dto.StartDate, dto.EndDate);

            var timeRange = dto.StartTime.HasValue && dto.EndTime.HasValue
                ? TimeRange.Create(dto.StartTime.Value, dto.EndTime.Value)
                : null;

            service.AddUnavailability(dateRange, timeRange, dto.Reason);

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        public async Task<Result> RemoveUnavailability(int id, int unavailabilityId, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithUnavailability(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.RemoveUnavailability(unavailabilityId);

            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);

            return Result.Success();
        }

        // ========== ACTIVATE / DEACTIVATE ==========
        public async Task<Result> Activate(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.Activate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }
        public async Task<Result> Deactivate(int id, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOne(id, ct);
            if (service == null)
                return Result.Failure(Error.NotFound("Servicio"));

            service.Deactivate();
            _serviceRepository.Update(service);
            await _unitOfWork.SaveChanges(ct);
            return Result.Success();
        }

        // GET SLOTS
        public async Task<Result<List<DateTime>>> GetAvailableSlots(int id, DateOnly date, CancellationToken ct = default)
        {
            var service = await _serviceRepository.GetOneWithSchedulesAndUnavailability(id, ct);
            if (service == null)
                return Result<List<DateTime>>.Failure(Error.NotFound("Servicio"));

            var appointments= await _appointmentRepository.GetByServiceAndDate(id, date, ct);

            var slots = _availabilityService.GetAvailableSlots(service, appointments, date);
            return Result<List<DateTime>>.Success(slots.ToList());
        }
    }
}