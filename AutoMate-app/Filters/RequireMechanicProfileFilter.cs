using AutoMate_app.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AutoMate_app.Filters
{
    public class RequireMechanicProfileFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public RequireMechanicProfileFilter(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;

            // Must be logged in
            if (http.User?.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            // Only apply to Mechanics
            if (!http.User.IsInRole("Mechanic"))
            {
                await next();
                return;
            }

            // Exclude Identity pages (Login/Logout/etc.)
            var path = http.Request.Path.Value ?? "";
            if (path.StartsWith("/Identity", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Exclude the profile completion page itself (avoid redirect loop)
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            if (string.Equals(controller, "MechanicProfiles", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(action, "Create", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            // Check if mechanic has a profile
            var userId = _userManager.GetUserId(http.User);

            var hasProfile = await _context.MechanicProfiles
                .AnyAsync(mp => mp.UserId == userId);

            if (!hasProfile)
            {
                context.Result = new RedirectToActionResult("Create", "MechanicProfiles", null);
                return;
            }

            await next();
        }
    }
}
