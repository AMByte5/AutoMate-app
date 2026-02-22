using System.ComponentModel.DataAnnotations;

namespace AutoMate_app.Models.ViewModels
{
    public class CreateServiceRequestVM
    {
        [Required]
        public int ServiceTypeId { get; set; }

        [Required, StringLength(500)]
        public string ProblemDescription { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string LocationAddress { get; set; } = string.Empty;

        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }

    }
}
