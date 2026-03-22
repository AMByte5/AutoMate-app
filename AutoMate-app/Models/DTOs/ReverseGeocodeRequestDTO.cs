using System.ComponentModel.DataAnnotations;

namespace AutoMate_app.Models.DTOs
{
    public record ReverseGeocodeRequestDTO(
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        double Lat,

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        double Lng
);

}
