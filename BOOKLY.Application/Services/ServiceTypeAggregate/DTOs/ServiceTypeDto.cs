namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record ServiceTypeDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public List<ServiceTypeFieldDefinitionDto> FieldDefinitions { get; init; } = new();
    }
}