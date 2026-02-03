using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoMate_app.Models
{
    public class MechanicProfile
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }   // FK to AspNetUsers

        [Required, StringLength(200)]
        public string GarageName { get; set; }

        [StringLength(200)]
        public string Specialization { get; set; }

        [Range(0, 5)]
        public double AverageRating { get; set; }

        public int TotalReviews { get; set; }

        public bool IsVerifiedByAdmin { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; }

    }
}
