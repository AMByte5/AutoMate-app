using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AutoMate_app.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public string ClientId { get; set; }  // FK to AspNetUsers

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public IdentityUser Client { get; set; }
        public ServiceRequest ServiceRequest { get; set; }
    }
}
