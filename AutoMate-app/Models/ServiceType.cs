using System.ComponentModel.DataAnnotations;

namespace AutoMate_app.Models
{
    public class ServiceType
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

    }
}
