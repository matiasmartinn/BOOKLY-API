namespace BOOKLY.Application.Services.PublicBooking.DTOs
{
    public sealed record PublicServiceFieldOptionDto
    {
        public int Id { get; init; }
        public string Value { get; init; } = null!;
        public string Label { get; init; } = null!;
        public int SortOrder { get; init; }
    }
}
