namespace BOOKLY.Application.Services.PublicBooking.DTOs
{
    public sealed record PublicServiceFieldDto
    {
        public int Id { get; init; }
        public string Key { get; init; } = null!;
        public string Label { get; init; } = null!;
        public string? Description { get; init; }
        public int FieldType { get; init; }
        public bool IsRequired { get; init; }
        public int SortOrder { get; init; }
        public List<PublicServiceFieldOptionDto> Options { get; init; } = [];
    }
}
