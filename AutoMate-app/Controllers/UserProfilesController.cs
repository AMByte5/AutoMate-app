using System.Linq;
using System.Threading.Tasks;
using AutoMate_app.Data;
using AutoMate_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoMate_app.Controllers
{
    [Authorize]
    public class UserProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserProfilesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: UserProfiles
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var profiles = await _context.UserProfiles
                .Include(up => up.User)
                .OrderBy(up => up.FullName)
                .ToListAsync();

            return View(profiles);
        }

        // GET: UserProfiles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.UserProfiles
                .Include(up => up.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (profile == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && profile.UserId != currentUserId)
                return Forbid();

            return View(profile);
        }

        // GET: UserProfiles/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var currentUserId = _userManager.GetUserId(User);

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == currentUserId);

            if (profile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = profile.Id });
        }

        // GET: UserProfiles/Create
        public async Task<IActionResult> Create()
        {
            var currentUserId = _userManager.GetUserId(User);

            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == currentUserId);

            if (existingProfile != null)
            {
                TempData["Info"] = "You already have a profile. You can edit it instead.";
                return RedirectToAction(nameof(Edit), new { id = existingProfile.Id });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);

            var model = new UserProfile
            {
                UserId = currentUserId,
                Email = currentUser?.Email,
                FullName = currentUser?.UserName
            };

            return View(model);
        }

        // POST: UserProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,PhoneNumber,Email,Address,City")] UserProfile userProfile)
        {
            var currentUserId = _userManager.GetUserId(User);
            userProfile.UserId = currentUserId;
            ModelState.Remove(nameof(UserProfile.UserId));
            ModelState.Remove(nameof(UserProfile.User));

            if (ModelState.IsValid)
            {
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == currentUserId);

                if (existingProfile != null)
                {
                    TempData["Error"] = "Profile already exists. Edit your profile instead.";
                    return RedirectToAction(nameof(Edit), new { id = existingProfile.Id });
                }

                _context.Add(userProfile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile created successfully.";
                return RedirectToAction(nameof(Details), new { id = userProfile.Id });
            }

            return View(userProfile);
        }

        // GET: UserProfiles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && profile.UserId != currentUserId)
                return Forbid();

            return View(profile);
        }

        // POST: UserProfiles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,FullName,PhoneNumber,Email,Address,City")] UserProfile userProfile)
        {
            if (id != userProfile.Id)
                return NotFound();

            var existingProfile = await _context.UserProfiles.AsNoTracking().FirstOrDefaultAsync(up => up.Id == id);
            if (existingProfile == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingProfile.UserId != currentUserId)
                return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userProfile);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Profile updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserProfileExists(userProfile.Id))
                        return NotFound();
                    else
                        throw;
                }

                if (isAdmin)
                    return RedirectToAction(nameof(Index));
                else
                    return RedirectToAction(nameof(Details), new { id = userProfile.Id });
            }

            return View(userProfile);
        }

        // GET: UserProfiles/Delete/5 (Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.UserProfiles
                .Include(up => up.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }

        // POST: UserProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profile deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserProfileExists(int id)
        {
            return _context.UserProfiles.Any(e => e.Id == id);
        }
    }
}