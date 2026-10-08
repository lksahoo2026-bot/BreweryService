using BreweryApi.App.Filters;
using BreweryApi.Models;
using BreweryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreweryApi.App.Controllers
{
    [ApiController]
    [Route("api/v1/brewery")]
    [ServiceFilter(typeof(GlobalExceptionFilter))]
    public class BreweryController : ControllerBase
    {
        // Service used to retrieve and process brewery data.
        private readonly IBreweryService _breweryService;

        // Logger used to log controller-level information.
        private readonly ILogger<BreweryController> _logger;

        public BreweryController(IBreweryService breweryService,ILogger<BreweryController> logger)
        {
            _breweryService = breweryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetBreweries([FromQuery] BreweryQuery query)
        {
            _logger.LogInformation(
            "Get breweries request received. Search: {Search}, SortBy: {SortBy}, Page: {Page}, PageSize: {PageSize}",
            query.Search,query.SortBy,query.Page,query.PageSize);

            // Validate page number.
            if (query.Page < 1)
            {
                return BadRequest("Page must be greater than zero.");
            }

            // Validate page size when it is provided.
            if (query.PageSize.HasValue && query.PageSize.Value < 1)
            {
                return BadRequest("PageSize must be greater than zero.");
            }

            // Validate sortBy only when it is provided
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                // Define the valid sorting values
                var validSortValues = new[] { "name", "city", "distance" };

                // Return 400 if an invalid sorting option is provided
                if (!validSortValues.Contains(query.SortBy, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        message = "Invalid sortBy value. Valid values are: name, city, distance."
                    });
                }
            }

            if (string.Equals(query.SortBy, "distance", StringComparison.OrdinalIgnoreCase) && (query.Latitude == null || query.Longitude == null))
            {
                return BadRequest("Latitude and Longitude are required when sorting by distance.");
            }

            // Call the service to get brewery data.
            var breweries = await _breweryService.GetBreweriesAsync(query);
            
            _logger.LogInformation("Get breweries request completed. Count: {Count}",breweries.Count);

            // Return the brewery data with HTTP 200 OK.
            return Ok(breweries);
        }
    }
}
