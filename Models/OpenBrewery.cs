namespace BreweryApi.Models
{
    /// <summary>
    /// Represents the brewery data received from the external Open Brewery API.
    /// </summary>
    public class OpenBrewery
    {
        // Name of the brewery 
        public string Name { get; set; } = string.Empty;

        // City where the brewery is located.
        public string City { get; set; } = string.Empty;

        // Phone number of the brewery.
        public string? Phone { get; set; }

        // Latitude of the brewery location.
        public double? Latitude { get; set; }

        // Longitude of the brewery location.
        public double? Longitude { get; set; }

    }
}
