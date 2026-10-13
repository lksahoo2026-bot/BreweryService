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

        /// <summary>
        ///  Search and sort breweries by name
        /// </summary>
        /// <returns></returns>
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
            Assert.NotEmpty(breweries);
            Assert.Equal("Austin", breweries[0].City);
            Assert.Equal("(512) Brewing Co", breweries[0].Name);            
        }

        /// <summary>
        /// Get breweries — returns success
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetBreweries_ReturnsSuccess()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/brewery");

            // Assert
            response.EnsureSuccessStatusCode();

            var breweries =
                await response.Content.ReadFromJsonAsync<List<BreweryResponse>>();

            Assert.NotNull(breweries);
        }

        /// <summary>
        /// Search breweries by city
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetBreweries_SearchByCity_ReturnsMatchingResults()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/brewery?search=Austin");

            // Assert
            response.EnsureSuccessStatusCode();

            var breweries =
                await response.Content.ReadFromJsonAsync<List<BreweryResponse>>();

            Assert.NotNull(breweries);
            Assert.NotEmpty(breweries);

            Assert.All(breweries, brewery =>
                Assert.Contains(
                    "Austin",
                    brewery.City ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Pagination — respects page size
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetBreweries_WithPageSize_ReturnsLimitedResults()
        {
            // Arrange
            const int pageSize = 5;

            // Act
            var response = await _client.GetAsync(
                $"/api/v1/brewery?page=1&pageSize={pageSize}");

            // Assert
            response.EnsureSuccessStatusCode();

            var breweries =
                await response.Content.ReadFromJsonAsync<List<BreweryResponse>>();

            Assert.NotNull(breweries);
            Assert.True(breweries.Count <= pageSize);
        }

        /// <summary>
        /// Invalid sort field — returns Bad Request
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetBreweries_InvalidSortBy_ReturnsBadRequest()
        {
            // Act
            var response = await _client.GetAsync(
                "/api/v1/brewery?sortBy=invalidField");

            // Assert
            Assert.Equal(
                System.Net.HttpStatusCode.BadRequest,
                response.StatusCode);
        }

    }
}
