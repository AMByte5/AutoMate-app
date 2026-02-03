using Microsoft.AspNetCore.Identity;

namespace AutoMate_app.Models
{
    public class AssignVM
    {
        public String Id { get; set; }
        public String UserId { get; set; }
        public String RoleId { get; set; }
        public IdentityUser User { get; set; }
        public IdentityRole Role { get; set; }

    }
}
