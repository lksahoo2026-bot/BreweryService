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
            var breweries =
            await _breweryService.GetBreweriesV2Async(query);

            var response = breweries.Select(b => new BreweryResponseV2
            {
                Name = b.Name,
                City = b.City,
                Phone = b.Phone,
                Country = b.Country
            }).ToList();

            return Ok(response);
        }


    }
}
