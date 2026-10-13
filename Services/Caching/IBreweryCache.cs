using BreweryApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Services.Caching
{
    public interface IBreweryCache
    {
        bool TryGetBreweries(
        string key,
        out List<OpenBrewery>? breweries);

        void SetBreweries(
            string key,
            List<OpenBrewery> breweries);
    }
}
