namespace BOOKLY.Application.Services.ServiceTypeAggregate.DTOs
{
    public sealed record UpdateServiceTypeDto
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
    }
}