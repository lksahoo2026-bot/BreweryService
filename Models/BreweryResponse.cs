using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Models
{
    /// <summary>
    /// Represents the brewery information returned to the client.
    /// Only the required brewery details(Name, City, Phone) are exposed.
    /// </summary>
    public class BreweryResponse
    {
        // Name of the brewery.
        public string Name { get; set; } = string.Empty;

        // City where the brewery is located.
        public string City { get; set; } = string.Empty;

        // Phone number of the brewery.
        public string? Phone { get; set; }
    }
}
