namespace BOOKLY.Application.Services.ServiceAggregate.DTOs
{
    public sealed record SetSecretariesDto
    {
        public List<int>? SecretaryIds { get; init; }
    }
}
