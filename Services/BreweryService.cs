using BreweryApi.Clients;
using BreweryApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Services
{
    public class BreweryService : IBreweryService
    {
        // Base key used for brewery cache entries.
        private const string CacheKey = "breweries";

        // In-memory cache used to store brewery data.
        private readonly IMemoryCache _cache;

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
            IMemoryCache cache,
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
            if (_cache.TryGetValue<List<OpenBrewery>>(cacheKey,out var breweries) && breweries != null)
            {
                _logger.LogInformation(
                    "Brewery data found in cache. Page: {Page}, PageSize: {PageSize}",
                    page,
                    pageSize);

                return breweries;
            }

            // Cache miss. Wait for the current cache refresh.
            await _cacheLock.WaitAsync();

            try
            {
                // Check cache again after acquiring the lock.
                if (_cache.TryGetValue<List<OpenBrewery>>(cacheKey,out breweries) && breweries != null)
                {
                    _logger.LogInformation(
                        "Brewery data was populated by another request.");

                    return breweries;
                }

                _logger.LogInformation(
                    "Brewery data not found in cache. Calling external API. Page: {Page}, PageSize: {PageSize}",
                    page,
                    pageSize);

                // Get the requested page from the external API.
                breweries = await _apiClient.GetBreweriesAsync(page, pageSize) ?? new List<OpenBrewery>();

                // Store the page in cache for 10 minutes.
                _cache.Set(cacheKey,breweries,TimeSpan.FromMinutes(10));

                _logger.LogInformation("Brewery data stored in cache for 10 minutes.");

                return breweries;
            }
            finally
            {
                // Always release the semaphore.
                _cacheLock.Release();
            }
        }

        // Calculates the approximate distance between the brewery location
        // and the latitude/longitude provided by the user.
        private static double CalculateDistance(
            OpenBrewery brewery,
            BreweryQuery query)
        {
            if (brewery.Latitude == null || 
                brewery.Longitude == null ||
                query.Latitude == null ||
                query.Longitude == null)
            {
                return double.MaxValue;
            }

            double latDifference =
                brewery.Latitude.Value - query.Latitude.Value;

            double lonDifference =
                brewery.Longitude.Value - query.Longitude.Value;

            var result = Math.Sqrt(
                (latDifference * latDifference) +
                (lonDifference * lonDifference));

            return result;
        }
    }
}
