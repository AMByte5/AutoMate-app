using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace AutoMate_app.Models
{

    public enum ServiceStatus { Pending, Accepted, Rejected, Completed }
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; }   // FK to AspNetUsers

        public string? MechanicId { get; set; }  // FK to AspNetUsers

        [Required]
        public int ServiceTypeId { get; set; }

        [Required, StringLength(500)]
        public string ProblemDescription { get; set; }

        [Required, StringLength(200)]
        public string LocationAddress { get; set; }

        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



        public ServiceStatus Status { get; set; }

        public IdentityUser Client { get; set; }
        public IdentityUser? Mechanic { get; set; }
        public ServiceType ServiceType { get; set; }

        public string? AiSuggestedServiceType { get; set; }
        public string? AiPossibleReasonsJson { get; set; }
        public string? AiUrgency { get; set; }
        public bool? AiRecommendTowing { get; set; }      // FIX: was object + internal set
        public DateTime? AiCalculatedAt { get; set; }
    }
}
