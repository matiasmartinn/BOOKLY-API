namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record UpdateServiceTypeFieldDto
    {
        public string? Label { get; init; }
        public string? Description { get; init; }
        public bool? IsRequired { get; init; }
        public int? SortOrder { get; init; }
        public int? FieldType { get; init; }
    }
}