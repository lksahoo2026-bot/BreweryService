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
        public async Task<List<OpenBrewery>> GetBreweriesAsync()
        {            
            _logger.LogInformation("Calling Open Brewery API.");

            // Call the external Brewery API.
            //v1/breweries
            HttpResponseMessage response =
                await _httpClient.GetAsync("v1/breweries?per_page=100");

            // Check whether the API call was successful.
            response.EnsureSuccessStatusCode();

            // Log the response status code.
            _logger.LogInformation("Open Brewery API returned status code: {StatusCode}",
                response.StatusCode);

            // Read the API response as JSON.
            string json = await response.Content.ReadAsStringAsync();

            // Convert the JSON response received from the external API
            // into a list of OpenBrewery objects.
            List<OpenBrewery>? breweries = JsonSerializer.Deserialize<List<OpenBrewery>>(json,
                new JsonSerializerOptions
                {
                    // Allows JSON and property names to match without considering uppercase or lowercase differences.
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
