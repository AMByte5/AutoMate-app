using AutoMate_app.Models.Options;
using AutoMate_app.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;


namespace AutoMate_app.Services;

public class LocationService : ILocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleMapsOptions _googleMapsOptions;
    private readonly IMemoryCache _cache;

    public LocationService(IHttpClientFactory httpClientFactory, IOptions<GoogleMapsOptions> googleMapsOptions, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _googleMapsOptions = googleMapsOptions.Value;
        _cache = cache;
    }

    public async Task<string?> ReverseGeocodeAsync(double lat, double lng)
    {
        var rLat = Math.Round(lat, 4);
        var rLng = Math.Round(lng, 4);
        var key = $"rev_geo_{rLat}_{rLng}";

        if (_cache.TryGetValue(key, out string? cachedAddress)) return cachedAddress;

        var client = _httpClientFactory.CreateClient();
        var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_googleMapsOptions.ApiKey}";

        try
        {
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            if (root.GetProperty("status").GetString() != "OK") return null;

            var results = root.GetProperty("results");
            if (results.GetArrayLength() == 0) return null;

            var address = results[0].GetProperty("formatted_address").GetString();

            if (address != null)
                _cache.Set(key, address, TimeSpan.FromHours(24));

            return address;
        }
        catch
        {
            return null;
        }
    }
}