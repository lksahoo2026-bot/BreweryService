using BreweryApi.Clients;
using BreweryApi.Models;
using BreweryApi.Services;
using BreweryApi.Services.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreweryApi.Tests;

public class BreweryServiceTests
{
    private readonly Mock<IBreweryApiClient> _apiClientMock;
    private readonly IBreweryCache _cache;
    private readonly Mock<ILogger<BreweryService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;

    private readonly BreweryService _service;

    public BreweryServiceTests()
    {
        _apiClientMock = new Mock<IBreweryApiClient>();
        _cache = new BreweryCache(new MemoryCache(new MemoryCacheOptions()));
        _loggerMock = new Mock<ILogger<BreweryService>>();

        _configurationMock = new Mock<IConfiguration>();

        _configurationMock
            .Setup(x => x["DefaultPageSize"])
            .Returns("100");

        _service = new BreweryService(
            _apiClientMock.Object,
            _cache,
            _loggerMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task GetBreweriesAsync_ReturnsBreweries()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
            new OpenBrewery
            {
                Name = "ABC Brewery",
                City = "New York",
                Phone = "123456789"
            }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("ABC Brewery", result[0].Name);
        Assert.Equal("New York", result[0].City);
    }

    [Fact]
    public async Task GetBreweriesAsync_SearchByName()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
            new OpenBrewery { Name = "ABC Brewery", City = "New York" },
            new OpenBrewery { Name = "XYZ Brewery", City = "Boston" }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            Search = "ABC"
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("ABC Brewery", result[0].Name);
    }

    [Fact]
    public async Task GetBreweriesAsync_SearchByCity_ReturnsMatchingBrewery()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
            new OpenBrewery { Name = "ABC Brewery", City = "New York" },
            new OpenBrewery { Name = "XYZ Brewery", City = "Boston" }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            Search = "Boston"
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("Boston", result[0].City);
    }

    [Fact]
    public async Task GetBreweriesAsync_SortsByName()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
            new OpenBrewery { Name = "Z Brewery", City = "New York" },
            new OpenBrewery { Name = "A Brewery", City = "Boston" }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            SortBy = "name"
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Equal("A Brewery", result[0].Name);
        Assert.Equal("Z Brewery", result[1].Name);
    }


    [Fact]
    public async Task GetBreweriesAsync_SortsByCity()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
    {
        new OpenBrewery { Name = "Z Brewery", City = "New York" },
        new OpenBrewery { Name = "A Brewery", City = "Boston" }
    };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            SortBy = "city"
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Boston", result[0].City);
        Assert.Equal("New York", result[1].City);
    }


    [Fact]
    public async Task GetBreweriesAsync_SortsByDistance()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
        new OpenBrewery { Name = "Far Brewery", Latitude = 40.7128, Longitude = -74.0060 },
        new OpenBrewery { Name = "Near Brewery", Latitude = 42.3601, Longitude = -71.0589 }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);


        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            SortBy = "distance",
            Latitude = 42.3601,
            Longitude = -71.0589
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Equal("Near Brewery", result[0].Name);
        Assert.Equal("Far Brewery", result[1].Name);
    }

    [Fact]
    public async Task GetBreweriesAsync_SearchesByCityAndSortsByName()
    {
        // Arrange
        var breweries = new List<OpenBrewery>
        {
        new OpenBrewery { Name = "Z Brewery", City = "Boston" },
        new OpenBrewery { Name = "A Brewery", City = "Boston" },
        new OpenBrewery { Name = "M Brewery", City = "New York" }
        };

        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100,
            Search = "Boston",
            SortBy = "name"
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A Brewery", result[0].Name);
        Assert.Equal("Z Brewery", result[1].Name);
        Assert.Equal("Boston", result[0].City);
        Assert.Equal("Boston", result[1].City);
    }

    [Fact]
    public async Task GetBreweriesAsync_ReturnsEmptyList_WhenApiReturnsEmpty()
    {
        // Arrange
        _apiClientMock
            .Setup(x => x.GetBreweriesAsync(1, 100))
            .ReturnsAsync(new List<OpenBrewery>());

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = await _service.GetBreweriesAsync(query);

        // Assert
        Assert.Empty(result);
    }
    
}