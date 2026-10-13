using BreweryApi.Clients;
using BreweryApi.Models;
using BreweryApi.Services.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Services
{
    public class BreweryService : IBreweryService
    {
        // Base key used for brewery cache entries.
        private const string CacheKey = "breweries";

        // IBreweryCache used to store brewery data.
        private readonly IBreweryCache _cache;

        // Client to call the external Brewery API.
        private readonly IBreweryApiClient _apiClient;

        // Logger used to log service-level information.
        private readonly ILogger<BreweryService> _logger;     

        private readonly IConfiguration _configuration;

        // Creates a semaphore that allows only one request at a time.
        //SemaphoreSlim is static so that concurrent requests share the same lock when the cache is being populated.
        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        public BreweryService(
            IBreweryApiClient apiClient,
            IBreweryCache cache,
            ILogger<BreweryService> logger,
            IConfiguration configuration)
        {
            _apiClient = apiClient;
            _cache = cache;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<List<BreweryResponse>> GetBreweriesAsync(
            BreweryQuery query)
        {
            _logger.LogInformation(
                "Brewery service processing started. Search: {Search}, SortBy: {SortBy}, Page: {Page}, PageSize: {PageSize}",
                query.Search, query.SortBy,query.Page,query.PageSize);

            // Use page 1 when the user does not provide a page number.
            int page = query.Page;

            // Use the page size provided by the user.
            // If not provided, use the value from configuration.
            int pageSize = query.PageSize ?? int.Parse(_configuration["DefaultPageSize"]!);

            // Create a separate cache key for each page and page size.
            string cacheKey = $"{CacheKey}_{page}_{pageSize}";

            // Get brewery data from cache or external API.
            List<OpenBrewery> breweries =
                await GetFromCacheAsync(cacheKey, page, pageSize);

            _logger.LogInformation(
                "Retrieved {Count} breweries from cache or external API.",
                breweries.Count);

            // Search breweries by name or city.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                breweries = breweries.Where(x =>
                    x.Name.Contains(
                        query.Search,
                        StringComparison.OrdinalIgnoreCase) ||
                    x.City.Contains(
                        query.Search,
                        StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Sort breweries by name by default.
            if (string.IsNullOrWhiteSpace(query.SortBy) ||
                query.SortBy.Equals(
                    "name",
                    StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.Name).ToList();
            }
            // Sort breweries by city.
            else if (query.SortBy.Equals(
                "city",
                StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.City).ToList();
            }
            // Sort breweries by distance.
            else if (query.SortBy.Equals(
                "distance",
                StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries
                    .OrderBy(x => CalculateDistance(x, query))
                    .ToList();
            }

            // Create a list for the API response.
            List<BreweryResponse> result = new List<BreweryResponse>();

            // Map the required fields.
            foreach (OpenBrewery brewery in breweries)
            {
                BreweryResponse response = new BreweryResponse();

                response.Name = brewery.Name ?? string.Empty;
                response.City = brewery.City ?? string.Empty;
                response.Phone = brewery.Phone ?? string.Empty;

                result.Add(response);
            }

            _logger.LogInformation(
                "Brewery response mapping completed. Count: {Count}",
                result.Count);

            return result;
        }
        private async Task<List<OpenBrewery>> GetFromCacheAsync(string cacheKey,int page,int pageSize)
        {
            // First cache check.
            if (_cache.TryGetBreweries(cacheKey, out var cachedBreweries) && cachedBreweries != null)
            {
                _logger.LogInformation(
                    "Brewery data found in cache. Page: {Page}, PageSize: {PageSize}",
                    page,
                    pageSize);

                return cachedBreweries;
            }

            // Cache miss. Wait for the current cache refresh.
            await _cacheLock.WaitAsync();

            try
            {
               // Check cache again after acquiring the lock.
                if (_cache.TryGetBreweries(cacheKey, out var breweries) && breweries != null)
                {
                    _logger.LogInformation("Brewery data was populated by another request.");

                    return breweries;

                }

                _logger.LogInformation(
                    "Brewery data not found in cache. Calling external API. Page: {Page}, PageSize: {PageSize}",
                    page,
                    pageSize);

                // Fetch brewery data from the external API.
                breweries = await _apiClient.GetBreweriesAsync(page, pageSize) ?? new List<OpenBrewery>();

                // Store the page in cache for 10 minutes.            
                _cache.SetBreweries(cacheKey, breweries);

                _logger.LogInformation("Brewery data stored in cache for 10 minutes.");

                return breweries;
            }
            finally
            {
                // Always release the semaphore.
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Calculates the great-circle distance between a brewery
        /// and the specified search location using the Haversine formula.
        /// </summary>
        /// <param name="brewery">Brewery containing latitude and longitude.</param>
        /// <param name="query">Search location containing latitude and longitude.</param>
        /// <returns>
        /// Distance in kilometers, or double.MaxValue if coordinates are missing.
        /// </returns>
        private static double CalculateDistance(
            OpenBrewery brewery,
            BreweryQuery query)
        {
            // Check whether all required coordinates are available.
            if (brewery.Latitude == null ||
                brewery.Longitude == null ||
                query.Latitude == null ||
                query.Longitude == null)
            {
                // Return the maximum value when distance cannot be calculated.
                return double.MaxValue;
            }

            // Earth's mean radius in kilometers.
            const double earthRadiusKm = 6371.0;

            // Convert brewery coordinates from degrees to radians.
            double breweryLatitude =
                ToRadians(brewery.Latitude.Value);

            double breweryLongitude =
                ToRadians(brewery.Longitude.Value);

            // Convert search location coordinates from degrees to radians.
            double queryLatitude =
                ToRadians(query.Latitude.Value);

            double queryLongitude =
                ToRadians(query.Longitude.Value);

            // Calculate the differences between the two coordinates.
            double latDifference =
                queryLatitude - breweryLatitude;

            double lonDifference =
                queryLongitude - breweryLongitude;

            // Calculate the square of half the latitude difference's sine,
            // plus the longitude component adjusted for Earth's curvature.
            double a =
                Math.Pow(Math.Sin(latDifference / 2), 2) +
                Math.Cos(breweryLatitude) *
                Math.Cos(queryLatitude) *
                Math.Pow(Math.Sin(lonDifference / 2), 2);

            // Keep the value within the valid range to prevent
            // floating-point rounding errors.
            a = Math.Clamp(a, 0.0, 1.0);

            // Calculate the central angle between the two locations in radians.
            double c = 2 * Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

            // Multiply the central angle by Earth's radius to get distance in km.
            return earthRadiusKm * c;
        }

        /// <summary>
        /// Converts an angle from degrees to radians.
        /// </summary>
        /// <param name="degrees">Angle expressed in degrees.</param>
        /// <returns>Angle expressed in radians.</returns>
        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public async Task<List<BreweryResponseV2>> GetBreweriesV2Async(
           BreweryQuery query)
        {
            _logger.LogInformation(
                "Brewery service processing started. Search: {Search}, SortBy: {SortBy}, Page: {Page}, PageSize: {PageSize}",
                query.Search, query.SortBy, query.Page, query.PageSize);

            // Use page 1 when the user does not provide a page number.
            int page = query.Page;

            // Use the page size provided by the user.
            // If not provided, use the value from configuration.
            int pageSize = query.PageSize ?? int.Parse(_configuration["DefaultPageSize"]!);


            // Create a separate cache key for each page and page size.
            string cacheKey = $"{CacheKey}_{page}_{pageSize}";

            // Get brewery data from cache or external API.
            List<OpenBrewery> breweries =
                await GetFromCacheAsync(cacheKey, page, pageSize);

            _logger.LogInformation(
                "Retrieved {Count} breweries from cache or external API.",
                breweries.Count);

            // Search breweries by name or city.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                breweries = breweries.Where(x =>
                    x.Name.Contains(
                        query.Search,
                        StringComparison.OrdinalIgnoreCase) ||
                    x.City.Contains(
                        query.Search,
                        StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Sort breweries by name by default.
            if (string.IsNullOrWhiteSpace(query.SortBy) ||
                query.SortBy.Equals(
                    "name",
                    StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.Name).ToList();
            }
            // Sort breweries by city.
            else if (query.SortBy.Equals(
                "city",
                StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.City).ToList();
            }
            // Sort breweries by distance.
            else if (query.SortBy.Equals(
                "distance",
                StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries
                    .OrderBy(x => CalculateDistance(x, query))
                    .ToList();
            }

            // Create a list for the API response.
            List<BreweryResponseV2> result = new List<BreweryResponseV2>();

            // Map the required fields.
            foreach (OpenBrewery brewery in breweries)
            {
                BreweryResponseV2 response = new BreweryResponseV2();

                response.Name = brewery.Name ?? string.Empty;
                response.City = brewery.City ?? string.Empty;
                response.Phone = brewery.Phone ?? string.Empty;
                response.Country = brewery.Country ?? string.Empty;

                result.Add(response);
            }

            _logger.LogInformation(
                "Brewery response mapping completed. Count: {Count}",
                result.Count);

            return result;
        }
       
    }
}
