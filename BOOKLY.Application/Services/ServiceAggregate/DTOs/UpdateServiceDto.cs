namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    /// <summary>
    /// DTO para actualizar un servicio existente
    /// </summary>
    public sealed record UpdateServiceDto
    {
        public string? Name { get; init; }
        public string? Slug { get; init; }
        public string? Description { get; init; }
        public string? PlaceName { get; init; }
        public string? Address { get; init; }
        public string? GoogleMapsUrl { get; init; }
        public int? ServiceTypeId { get; init; }
        public int? DurationMinutes { get; init; }
        public int? Capacity { get; init; }
        public int? Mode { get; init; }
        public decimal? Price { get; init; }
    }
}
