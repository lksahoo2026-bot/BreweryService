using BreweryApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Clients
{
    public interface IBreweryApiClient
    {
        Task<List<OpenBrewery>> GetBreweriesAsync();
    }
}
