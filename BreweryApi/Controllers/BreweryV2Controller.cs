using Asp.Versioning;
using BreweryApi.App.Filters;
using BreweryApi.Models;
using BreweryApi.Services;
using Microsoft.AspNetCore.Mvc;


namespace BreweryApi.App.Controllers
{
    [ApiController]    
    [Route("api/v2/brewery")]
    [ServiceFilter(typeof(GlobalExceptionFilter))]
    public class BreweryV2Controller : ControllerBase
    {
        // Service used to retrieve and process brewery data.
        private readonly IBreweryService _breweryService;

        // Logger used to log controller-level information.
        private readonly ILogger<BreweryV2Controller> _logger;
        public BreweryV2Controller(IBreweryService breweryService, ILogger<BreweryV2Controller> logger)
        {
            _breweryService = breweryService;
            _logger = logger;
        }


        [HttpGet]
        public async Task<IActionResult> GetBreweries(
            [FromQuery] BreweryQuery query)
        {
            _logger.LogInformation(
            "Get breweries request received. Search: {Search}, SortBy: {SortBy}, Page: {Page}, PageSize: {PageSize}",
            query.Search, query.SortBy, query.Page, query.PageSize);

            // Validate page number.
            if (query.Page < 1)
            {
                return BadRequest("Page must be greater than zero.");
            }

            // Validate page size when it is provided.
            if (query.PageSize.HasValue && (query.PageSize.Value < 1 || query.PageSize.Value > 200))
            {
                return BadRequest("PageSize must be between 1 and 200.");
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
            var response = await _breweryService.GetBreweriesV2Async(query);

            return Ok(response);
        }


    }
}
