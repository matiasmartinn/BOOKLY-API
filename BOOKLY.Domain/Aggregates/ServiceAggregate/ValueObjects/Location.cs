namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public sealed record Location
    {
        public string? PlaceName { get; init; } = null!;
        public string? Address { get; init; } = null!;

        private Location(string? placeName, string? address)
        {
            PlaceName = CleanOptionalText(placeName);
            Address = CleanOptionalText(address);
        }

        public static Location Create(string? placeName, string? address)
        {
            return new Location(placeName, address);
        }

        private static string? CleanOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }
    }
}
