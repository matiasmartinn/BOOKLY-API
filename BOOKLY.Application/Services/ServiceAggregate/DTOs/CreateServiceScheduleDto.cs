namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    // <summary>
    /// DTO para crear horarios al crear un servicio
    /// </summary>
    public sealed record CreateServiceScheduleDto
    {
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
        public int? Capacity { get; init; }
        public int Day { get; init; } // 0 = Domingo, 1 = Lunes, etc.
    }
}
