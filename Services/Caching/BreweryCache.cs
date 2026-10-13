using BreweryApi.Models;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BreweryApi.Services.Caching
{
    public class BreweryCache : IBreweryCache
    {
        private readonly IMemoryCache _cache;

        // Cache duration is 10 minutes.
        private static readonly TimeSpan CacheDuration =
            TimeSpan.FromMinutes(10);

        public BreweryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryGetBreweries(
            string key,
            out List<OpenBrewery>? breweries)
        {
            return _cache.TryGetValue(key, out breweries);
        }

        public void SetBreweries(
            string key,
            List<OpenBrewery> breweries)
        {
            _cache.Set(key, breweries, CacheDuration);
        }
    }
}