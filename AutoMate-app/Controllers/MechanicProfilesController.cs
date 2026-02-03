using System.Linq;
using System.Threading.Tasks;
using AutoMate_app.Data;
using AutoMate_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMate_app.Controllers
{
    [Authorize]
    public class MechanicProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MechanicProfilesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MechanicProfiles
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, bool showVerifiedOnly = true, string sortOrder = "rating_desc")
        {
            var mechanics = _context.MechanicProfiles
                .Include(mp => mp.User)
                .AsQueryable();

            if (showVerifiedOnly)
            {
                mechanics = mechanics.Where(mp => mp.IsVerifiedByAdmin);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                mechanics = mechanics.Where(mp =>
                    mp.GarageName.Contains(searchString) ||
                    mp.Specialization.Contains(searchString) ||
                    mp.User.Email.Contains(searchString));
            }

            mechanics = sortOrder switch
            {
                "rating_asc" => mechanics.OrderBy(mp => mp.AverageRating),
                _ => mechanics.OrderByDescending(mp => mp.AverageRating)
            };

            ViewBag.SearchString = searchString;
            ViewBag.ShowVerifiedOnly = showVerifiedOnly;
            ViewBag.SortOrder = sortOrder;

            return View(await mechanics.ToListAsync());
        }

        // GET: MechanicProfiles/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.MechanicProfiles
                .Include(mp => mp.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }

        // GET: MechanicProfiles/MyProfile
        [Authorize(Roles = "Mechanic,Admin")]
        public async Task<IActionResult> MyProfile()
        {
            var currentUserId = _userManager.GetUserId(User);
            var profile = await _context.MechanicProfiles
                .FirstOrDefaultAsync(mp => mp.UserId == currentUserId);

            if (profile == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = profile.Id });
        }

        // GET: MechanicProfiles/Create

        public async Task<IActionResult> Create()
        {
            var currentUserId = _userManager.GetUserId(User);
            var existingProfile = await _context.MechanicProfiles
                .FirstOrDefaultAsync(mp => mp.UserId == currentUserId);

            if (existingProfile != null)
            {
                TempData["Info"] = "You already have a mechanic profile. Edit it instead.";
                return RedirectToAction(nameof(Edit), new { id = existingProfile.Id });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var model = new MechanicProfile
            {
                UserId = currentUserId,
                GarageName = $"{currentUser?.UserName}'s Garage",
                AverageRating = 0,
                TotalReviews = 0
            };

            return View(model);
        }

        // POST: MechanicProfiles/Create
        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GarageName,Specialization")] MechanicProfile mechanicProfile)
        {
            var currentUserId = _userManager.GetUserId(User);
            mechanicProfile.UserId = currentUserId;
            mechanicProfile.AverageRating = 0;
            mechanicProfile.TotalReviews = 0;
            ModelState.Remove(nameof(MechanicProfile.UserId));
            ModelState.Remove(nameof(MechanicProfile.User));

            if (ModelState.IsValid)
            {
                var existingProfile = await _context.MechanicProfiles
                    .FirstOrDefaultAsync(mp => mp.UserId == currentUserId);

                if (existingProfile != null)
                {
                    TempData["Error"] = "Mechanic profile already exists. Edit your profile instead.";
                    return RedirectToAction(nameof(Edit), new { id = existingProfile.Id });
                }

                _context.Add(mechanicProfile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Mechanic profile created. Pending admin verification.";
                return RedirectToAction(nameof(Details), new { id = mechanicProfile.Id });
            }

            return View(mechanicProfile);
        }

        // GET: MechanicProfiles/Edit/5
        [Authorize(Roles = "Mechanic,Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.MechanicProfiles.FindAsync(id);
            if (profile == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && profile.UserId != currentUserId)
                return Forbid();

            return View(profile);
        }

        // POST: MechanicProfiles/Edit/5
        [HttpPost]
        [Authorize(Roles = "Mechanic,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,GarageName,Specialization,AverageRating,TotalReviews,IsVerifiedByAdmin")] MechanicProfile mechanicProfile)
        {
            if (id != mechanicProfile.Id)
                return NotFound();

            var existingProfile = await _context.MechanicProfiles.AsNoTracking().FirstOrDefaultAsync(mp => mp.Id == id);
            if (existingProfile == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && existingProfile.UserId != currentUserId)
                return Forbid();

            if (!isAdmin)
            {
                mechanicProfile.IsVerifiedByAdmin = existingProfile.IsVerifiedByAdmin;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mechanicProfile);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Mechanic profile updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MechanicProfileExists(mechanicProfile.Id))
                        return NotFound();
                    else
                        throw;
                }

                if (isAdmin)
                    return RedirectToAction(nameof(Index));
                else
                    return RedirectToAction(nameof(Details), new { id = mechanicProfile.Id });
            }

            return View(mechanicProfile);
        }

        // POST: MechanicProfiles/Verify/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id, bool verify = true)
        {
            var profile = await _context.MechanicProfiles.FindAsync(id);
            if (profile == null)
                return NotFound();

            profile.IsVerifiedByAdmin = verify;
            _context.Update(profile);
            await _context.SaveChangesAsync();

            TempData["Success"] = verify ? "Mechanic verified." : "Mechanic verification removed.";
            return RedirectToAction(nameof(Details), new { id = profile.Id });
        }

        // GET: MechanicProfiles/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var profile = await _context.MechanicProfiles
                .Include(mp => mp.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (profile == null)
                return NotFound();

            return View(profile);
        }

        // POST: MechanicProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mechanicProfile = await _context.MechanicProfiles.FindAsync(id);
            if (mechanicProfile != null)
            {
                _context.MechanicProfiles.Remove(mechanicProfile);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Mechanic profile deleted.";
            return RedirectToAction(nameof(Index));
        }

        private bool MechanicProfileExists(int id)
        {
            return _context.MechanicProfiles.Any(e => e.Id == id);
        }
    }
}