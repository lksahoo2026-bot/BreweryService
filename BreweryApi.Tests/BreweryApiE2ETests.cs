using BreweryApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using Xunit;
namespace BreweryApi.Tests
{
    public class BreweryApiE2ETests
    : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BreweryApiE2ETests(
            WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetBreweries_SearchAndSort_ReturnsSuccess()
        {
            // Act
            var response = await _client.GetAsync(
                "/api/v1/brewery?search=Austin&sortBy=name&page=1&pageSize=10");

            // Assert
            response.EnsureSuccessStatusCode();

            var breweries =  await response.Content.ReadFromJsonAsync<List<BreweryResponse>>();

            Assert.NotNull(breweries);
            Assert.Equal("Austin", breweries[0].City);            
        }
    }
}
