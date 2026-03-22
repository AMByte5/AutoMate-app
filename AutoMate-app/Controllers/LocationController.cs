using AutoMate_app.Models.DTOs;
using AutoMate_app.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoMate_app.Controllers
{
    [ApiController]
    [Route("api/location")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpPost("reverse-geocode")]
        public async Task<ActionResult<ReverseGeocodeResponseDTO>> ReverseGeocode([FromBody] ReverseGeocodeRequestDTO request)
        {
            var address = await _locationService.ReverseGeocodeAsync(request.Lat, request.Lng);

            return Ok(new ReverseGeocodeResponseDTO(address));
        }
    }
}