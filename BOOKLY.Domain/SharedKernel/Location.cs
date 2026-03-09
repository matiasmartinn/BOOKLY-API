
namespace BOOKLY.Domain.SharedKernel
{
    public class Location
    {
        public int Id { get; set; }
        public string? Address { get; private set; }
        public string? City { get; private set; }
        public string? PostalCode { get; private set; }
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public bool IsVirtual { get; private set; }
        public string? Platform { get; private set; }

        private Location() { }

        public static Location Create(
            bool isVirtual,
            string? address,
            string? city,
            string? postalCode,
            decimal? latitude,
            decimal? longitude,
            string? platform
            )
        {
            return new Location
            {
                IsVirtual = isVirtual,
                Address = address?.Trim(),
                City = city?.Trim(),
                PostalCode = postalCode,
                Latitude = latitude,
                Longitude = longitude,
                Platform = isVirtual ? platform : null,
            };
        }

        public void Update(
        string? address = null,
        string? city = null,
        string? postalCode = null,
        decimal? latitude = null,
        decimal? longitude = null,
        string? platform = null)
        {
            Address = address?.Trim();
            City = city?.Trim();
            PostalCode = postalCode;
            Latitude = latitude;
            Longitude = longitude;
            Platform = IsVirtual ? platform : null;
        }

        public void SetAsVirtual(string? platform = "Google Meet")
        {
            IsVirtual = true;
            Platform = platform;
            Address = null;
            City = null;
            PostalCode = null;
            Latitude = null;
            Longitude = null;
        }

        public void SetAsPhysical(string address, string city, string? postalCode = null, decimal? lat = null, decimal? lng = null)
        {
            IsVirtual = false;
            Platform = null;
            Address = address;
            City = city;
            PostalCode = postalCode;
            Latitude = lat;
            Longitude = lng;
        }
    }
}
