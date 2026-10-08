using BreweryApi.App.Controllers;
using BreweryApi.Models;
using BreweryApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BreweryApi.Tests;

public class BreweryControllerTests
{
    private readonly Mock<IBreweryService> _serviceMock;
    private readonly Mock<ILogger<BreweryController>> _loggerMock;

    private readonly BreweryController _controller;

    public BreweryControllerTests()
    {
        _serviceMock = new Mock<IBreweryService>();
        _loggerMock = new Mock<ILogger<BreweryController>>();

        _controller = new BreweryController(
            _serviceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetBreweries_ReturnsOk()
    {
        // Arrange
        var breweries = new List<BreweryResponse>
        {
            new BreweryResponse
            {
                Name = "ABC Brewery",
                City = "New York",
                Phone = "123456789"
            }
        };

        _serviceMock
            .Setup(x => x.GetBreweriesAsync(It.IsAny<BreweryQuery>()))
            .ReturnsAsync(breweries);

        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 100
        };

        // Act
        var result = await _controller.GetBreweries(query);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var returnedBreweries =
            Assert.IsType<List<BreweryResponse>>(okResult.Value);

        Assert.Single(returnedBreweries);
        Assert.Equal("ABC Brewery", returnedBreweries[0].Name);
    }

    [Fact]
    public async Task GetBreweries_InvalidPage_ReturnsBadRequest()
    {
        // Arrange
        var query = new BreweryQuery
        {
            Page = 0,
            PageSize = 100
        };

        // Act
        var result = await _controller.GetBreweries(query);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Page must be greater than zero.",badRequest.Value);
    }

    [Fact]
    public async Task GetBreweries_InvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        var query = new BreweryQuery
        {
            Page = 1,
            PageSize = 500
        };

        // Act
        var result = await _controller.GetBreweries(query);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal("PageSize must be between 1 and 200.",badRequest.Value);
    }
}