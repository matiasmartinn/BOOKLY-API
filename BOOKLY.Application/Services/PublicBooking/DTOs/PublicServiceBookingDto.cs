namespace BOOKLY.Application.Services.PublicBooking.DTOs
{
    public sealed record PublicServiceBookingDto
    {
        public int ServiceId { get; init; }
        public string Slug { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? OwnerName { get; init; }
        public string? Description { get; init; }
        public string? PlaceName { get; init; }
        public string? Address { get; init; }
        public string? GoogleMapsUrl { get; init; }
        public int ServiceTypeId { get; init; }
        public int DurationMinutes { get; init; }
        public int Capacity { get; init; }
        public string Mode { get; init; } = null!;
        public decimal? Price { get; init; }
        public List<PublicServiceFieldDto> FieldDefinitions { get; init; } = [];
    }
}
