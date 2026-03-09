namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record UpdateServiceTypeFieldOptionDto
    {
        public string? Label { get; init; }
        public int? SortOrder { get; init; }
    }
}