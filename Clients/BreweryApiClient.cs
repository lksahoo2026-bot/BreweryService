using BreweryApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BreweryApi.Clients
{
    public class BreweryApiClient : IBreweryApiClient
    {
        // HttpClient used to call the external Brewery API.
        private readonly HttpClient _httpClient;

        // Logger used to log external API communication.
        private readonly ILogger<BreweryApiClient> _logger;

        public BreweryApiClient(HttpClient httpClient,ILogger<BreweryApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<List<OpenBrewery>> GetBreweriesAsync(
        int page,
        int pageSize)
        {
            _logger.LogInformation(
                "Calling Open Brewery API. Page: {Page}, PageSize: {PageSize}",page, pageSize);

            // Call the external Brewery API with pagination.
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    $"v1/breweries?page={page}&per_page={pageSize}");

            // Check whether the API call was successful.
            response.EnsureSuccessStatusCode();

            // Log the response status code.
            _logger.LogInformation(
                "Open Brewery API returned status code: {StatusCode}",
                response.StatusCode);

            // Read the API response as JSON.
            string json = await response.Content.ReadAsStringAsync();

            // Convert JSON into OpenBrewery objects.
            List<OpenBrewery>? breweries =
                JsonSerializer.Deserialize<List<OpenBrewery>>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            // Log the number of breweries received.
            _logger.LogInformation(
                "Received {Count} breweries from Open Brewery API.",
                breweries?.Count ?? 0);

            // Return the brewery list.
            return breweries ?? new List<OpenBrewery>();
        }
    }
}
