using BreweryApi.Models;

namespace BreweryApi.Services
{
    public interface IBreweryService
    {
        Task<List<BreweryResponse>> GetBreweriesAsync(BreweryQuery query);
    }
}
