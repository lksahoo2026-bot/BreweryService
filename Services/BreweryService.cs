using BreweryApi.Clients;
using BreweryApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Services
{
    public class BreweryService : IBreweryService
    {
        // Key used to store and retrieve brewery data from the memory cache.
        private const string CacheKey = "breweries";
        // In-memory cache used to store brewery data.
        private readonly IMemoryCache _cache;

        // Client to call the external Brewery API.
        private readonly IBreweryApiClient _apiClient;

        // Logger used to log service-level information.
        private readonly ILogger<BreweryService> _logger;

        // creates a semaphore that allows only one request at a time
        private readonly SemaphoreSlim _cacheLock =  new SemaphoreSlim(1, 1);

        public BreweryService(IBreweryApiClient apiClient,IMemoryCache cache,ILogger<BreweryService> logger)
        {
            _apiClient = apiClient;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets breweries from the cache or from API, applies search (name or city) and sorting (by name or city or distance),
        /// and maps the data to the response model.        
        /// </summary>
        /// <param name="query">The query parameters specifying filter and sort options.</param>
        /// <returns>A list of breweries matching the specified criteria.</returns>
        public async Task<List<BreweryResponse>> GetBreweriesAsync(BreweryQuery query)
        {
            _logger.LogInformation("Brewery service processing started. Search: {Search}, SortBy: {SortBy}", query.Search, query.SortBy);

            // Get brewery data from the in-memory cache.
            // If data is not available, it will be fetched from the external API.
            List<OpenBrewery> breweries = await GetFromCacheAsync();

            // Log the number of breweries retrieved.
            _logger.LogInformation("Retrieved {Count} breweries from cache or external API.",
                breweries.Count);

            // Search breweries by name or city.
            // StringComparison.OrdinalIgnoreCase makes the search case-insensitive.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                breweries = breweries.Where(x =>
                    x.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    x.City.Contains(query.Search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // sort the breweries by name by default.
            if (string.IsNullOrWhiteSpace(query.SortBy) || query.SortBy.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.Name).ToList();
            }
            // Sort breweries by city.
            else if (query.SortBy.Equals("city", StringComparison.OrdinalIgnoreCase))
            {
                breweries = breweries.OrderBy(x => x.City).ToList();
            }
            // Sort breweries by distance.
            else if (query.SortBy.Equals("distance", StringComparison.OrdinalIgnoreCase))
            {          
                // Calculate the distance for each brewery and sort from nearest to farthest.
                breweries = breweries.OrderBy(x => CalculateDistance(x, query)).ToList();
            }

            // Create a list for the API response.
            List<BreweryResponse> result = new List<BreweryResponse>();

            // Map the required fields from OpenBrewery to BreweryResponse.
            // Only Name, City and Phone are returned to the client.
            foreach (OpenBrewery brewery in breweries)
            {
                BreweryResponse response = new BreweryResponse();
                response.Name = brewery.Name ?? string.Empty;
                response.City = brewery.City ?? string.Empty;
                response.Phone = brewery.Phone ?? string.Empty;
                result.Add(response);
            }

            // Log that mapping has completed.
            _logger.LogInformation("Brewery response mapping completed. Count: {Count}",result.Count);

            // Return the list of breweryresponse to the controller.
            return result;
        }

        /// <summary>
        /// Gets brewery data from the in-memory cache.
        /// If the data is not available in the cache, it retrieves the data
        /// from the external API data source and stores it in the cache for 10 minutes.
        /// </summary>
        /// <returns>Return the brewery data.</returns>
        private async Task<List<OpenBrewery>> GetFromCacheAsync()
        {
            // First cache check - avoids acquiring the lock when
            // the data is already available.            
            if (_cache.TryGetValue<List<OpenBrewery>>(CacheKey, out var breweries) && breweries != null)
            {
                _logger.LogInformation("Brewery data found in cache.");
                return breweries;
            }

            // Cache miss. Wait until the current cache refresh is completed.
            await _cacheLock.WaitAsync();

            try
            {
                // Double-check the cache after acquiring the lock.
                //
                // Another request may have already populated the cache
                // while this request was waiting for the lock.
                if (_cache.TryGetValue<List<OpenBrewery>>(CacheKey, out breweries) && breweries != null)
                {
                    _logger.LogInformation(
                        "Brewery data was populated by another request while waiting for cache lock.");

                    return breweries;
                }

                _logger.LogInformation(
                    "Brewery data not found in cache. Calling external API.");

                // Only one request will reach this point.
                //breweries = await _apiClient.GetBreweriesAsync();
                breweries = await _apiClient.GetBreweriesAsync() ?? new List<OpenBrewery>();

                // Store brewery data in cache for 10 minutes.
                _cache.Set(CacheKey,breweries,TimeSpan.FromMinutes(10));

                _logger.LogInformation(
                    "Brewery data stored in cache for 10 minutes.");

                return breweries;
            }
            finally
            {
                // Always release the semaphore, even if the external API
                // call throws an exception.
                _cacheLock.Release();
            }
        }
        /// <summary>
        /// Calculates a simple distance value between the brewery
        /// location and the location provided in the query.
        /// This value is used only for sorting breweries by distance.
        /// </summary>
        /// <param name="brewery">contains the brewery's latitude and longitude.</param>
        /// <param name="query">contains the user's latitude and longitude.</param>
        /// <returns>calculated distance value.</returns>
        private static double CalculateDistance(OpenBrewery brewery, BreweryQuery query)
        {
            // Check whether all required latitude and longitude values are available.
            if (brewery.Latitude == null || brewery.Longitude == null ||
                query.Latitude == null || query.Longitude == null)
            {
                return double.MaxValue;
            }
            // Calculate the difference between the brewery latitude and user latitude.
            double latDifference = brewery.Latitude.Value - query.Latitude.Value;

            // Calculate the difference between the brewery longitude and user longitude.
            double lonDifference = brewery.Longitude.Value - query.Longitude.Value;

            //Calculate and return a distance value.
            var result = Math.Sqrt(
                (latDifference * latDifference) +
                (lonDifference * lonDifference));
            return result;
        }
    }
}
