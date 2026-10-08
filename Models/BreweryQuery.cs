using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Models
{
    /// <summary>
    /// Contains the search, sorting and distance parameters
    /// provided by the client.
    /// </summary>
    public class BreweryQuery
    {
        // Search breweries by name or city.
        public string? Search { get; set; }

        // Sort breweries by name, city or distance.
        // Default sorting is by name.
        public string? SortBy { get; set; } = "name";

        // Latitude of the user's location, used for distance sorting.
        public double? Latitude { get; set; }

        // Longitude of the user's location, used for distance sorting.
        public double? Longitude { get; set; }

        // Page number. If the user does not provide it, page 1 is used.
        public int Page { get; set; } = 1;

        // Page size. If the user does not provide it, the configured default is used.
        public int? PageSize { get; set; }
    }
}
