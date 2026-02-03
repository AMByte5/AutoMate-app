using AutoMate_app.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoMate_app.Controllers
{
    public class AssignController : Controller
    {
        public RoleManager<IdentityRole> _roleManager { get; }
        public UserManager<IdentityUser> _userManager { get; }
        public AssignController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        // GET: AssignController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AssignController/Details/5
        public ActionResult Details(string id)
        {
            return View();
        }


        // GET: AssignController/Create
        public IActionResult Create()
        {
            ViewBag.userList = new SelectList(_userManager.Users.ToList(), "Id", "Email");
            ViewBag.rolesList = new SelectList(_roleManager.Roles.ToList(), "Id", "Name");
            return View(new AssignVM());
        }

        // POST: AssignController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssignVM model)
        {
            // Always repopulate lists when returning the view
            ViewBag.userList = new SelectList(_userManager.Users.ToList(), "Id", "Email");
            ViewBag.rolesList = new SelectList(_roleManager.Roles.ToList(), "Id", "Name");

            // Basic guard (since your model currently has no [Required])
            if (string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.RoleId))
            {
                ModelState.AddModelError("", "Please select both a user and a role.");
                return View(model);
            }

            var selectedUser = await _userManager.FindByIdAsync(model.UserId);
            var selectedRole = await _roleManager.FindByIdAsync(model.RoleId);

            if (selectedUser == null || selectedRole == null)
            {
                ModelState.AddModelError("", "User or role not found.");
                return View(model);
            }

            // Duplicate prevention (best UX)
            if (await _userManager.IsInRoleAsync(selectedUser, selectedRole.Name))
            {
                ModelState.AddModelError("", "User already has this role.");
                return View(model);
            }

            var result = await _userManager.AddToRoleAsync(selectedUser, selectedRole.Name);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to assign role.");
                // Optional logging: result.Errors
                return View(model);
            }

            TempData["Success"] = $"Assigned {selectedRole.Name} to {selectedUser.Email}.";

            return RedirectToAction(nameof(Create));
        }

        // GET: AssignController/Edit/5
        public ActionResult Edit(string id)
        {
            return View();
        }

        // POST: AssignController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AssignController/Delete/5
        public ActionResult Delete(string id)
        {
            return View();
        }

        // POST: AssignController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
