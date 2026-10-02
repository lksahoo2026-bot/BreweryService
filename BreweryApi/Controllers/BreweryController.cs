using BreweryApi.App.Fliters;
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
            _logger.LogInformation("Get breweries request received. Search: {Search}, SortBy: {SortBy}",
                query.Search,
                query.SortBy);

            if (query.SortBy == "distance" && (query.Latitude == null || query.Longitude == null))
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
