// BOOKLY.Application.Services.ServiceTypeAggregate.DTOs/ServiceTypeFieldDefinitionDto.cs
namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record ServiceTypeFieldOptionDto
    {
        public int Id { get; init; }
        public int FieldDefinitionId { get; init; }
        public string Value { get; init; } = null!;
        public string Label { get; init; } = null!;
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
    }
}