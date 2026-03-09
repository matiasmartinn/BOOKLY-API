namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record ServiceTypeFieldDefinitionDto
    {
        public int Id { get; init; }
        public int ServiceTypeId { get; init; }
        public string Key { get; init; } = null!;
        public string Label { get; init; } = null!;
        public string? Description { get; init; }
        public int FieldType { get; init; }
        public bool IsRequired { get; init; }
        public bool IsActive { get; init; }
        public int SortOrder { get; init; }
        public DateTime CreatedOn { get; init; }
        public DateTime? UpdatedOn { get; init; }
        public List<ServiceTypeFieldOptionDto> Options { get; init; } = new();
    }
}