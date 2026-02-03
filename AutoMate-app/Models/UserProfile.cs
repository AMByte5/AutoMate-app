using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AutoMate_app.Models
{
    public class UserProfile
    {
        public int Id { get; set; }


        [Required]
        public string UserId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }
        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        public IdentityUser User { get; set; }

    }
}
