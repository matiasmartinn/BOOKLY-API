namespace BOOKLY.Application.Services.ClientAggregate.DTOs
{
    public sealed record ClientListItemDto
    {
        public string Name { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string Phone { get; init; } = null!;
        public int TotalAppointments { get; init; }
        public DateTime? LastAppointmentDateTime { get; init; }
        public DateTime? NextAppointmentDateTime { get; init; }
    }
}
