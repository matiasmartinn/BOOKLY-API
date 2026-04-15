namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record ServicePublicBookingDto
    {
        public int ServiceId { get; init; }
        public string Slug { get; init; } = null!;
        public bool IsEnabled { get; init; }
        public string PublicBookingCode { get; init; } = null!;
        public DateTime? PublicBookingCodeUpdatedAt { get; init; }
        public string PublicUrl { get; init; } = null!;
    }
}
