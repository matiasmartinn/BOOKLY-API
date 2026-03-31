using BOOKLY.Domain.Exceptions;

namespace BOOKLY.Domain.Aggregates.ServiceAggregate.ValueObjects
{
    public sealed record Location
    {
        public string? PlaceName { get; init; } = null!;
        public string? Address { get; init; } = null!;
        public string? GoogleMapsUrl { get; init; } = null!;

        private Location(string? placeName, string? address, string? googleMapsUrl)
        {
            PlaceName = CleanOptionalText(placeName);
            Address = CleanOptionalText(address);
            GoogleMapsUrl = CleanOptionalUrl(googleMapsUrl);
        }

        public static Location Create(string? placeName, string? address, string? googleMapsUrl)
        {
            return new Location(placeName, address, googleMapsUrl);
        }

        private static string? CleanOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim();
        }

        private static string? CleanOptionalUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out _))
                throw new DomainException("La URL de Google Maps no es válida.");

            return trimmed;
        }

    }
}
