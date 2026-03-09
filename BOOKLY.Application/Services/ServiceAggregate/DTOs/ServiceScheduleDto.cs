namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    /// <summary>
    /// DTO para horarios del servicio
    /// </summary>
    public sealed record ServiceScheduleDto
    {
        public int Id { get; init; }
        public TimeOnly StartTime { get; init; }
        public TimeOnly EndTime { get; init; }
        public int Capacity { get; init; }
        public string Day { get; init; } = null!; // "Lunes", "Martes", etc.
        public int DayValue { get; init; } // 0-6 para ordenamiento
    }
}
