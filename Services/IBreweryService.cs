using BreweryApi.Models;

namespace BreweryApi.Services
{
    public interface IBreweryService
    {
        Task<List<BreweryResponse>> GetBreweriesAsync(BreweryQuery query);
        Task<List<BreweryResponseV2>> GetBreweriesV2Async(BreweryQuery query);
    }
}
