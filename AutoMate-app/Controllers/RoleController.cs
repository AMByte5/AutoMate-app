using AutoMate_app.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoMate_app.Controllers
{
    public class RoleController : Controller
    {
        public RoleManager<IdentityRole> _roleManager { get; }

        public RoleController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }


        // GET: RoleController
        public ActionResult Index()
        {
            var rolesList = _roleManager.Roles.Select(x => new RoleVM { Id = x.Id, RoleName = x.Name })
            .ToList();
            return View(rolesList);
        }


        // GET: RoleController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RoleController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleVM formRole)
        {
            ModelState.Remove(nameof(formRole.Id)); // or "Id"

            if (!ModelState.IsValid) return View(formRole);

            var role = new IdentityRole(formRole.RoleName);
            var result = await _roleManager.CreateAsync(role);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(formRole);
        }

        // GET: Role/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var vm = new RoleVM
            {
                Id = role.Id,
                RoleName = role.Name
            };

            return View(vm); // Views/Role/Edit.cshtml
        }

        // POST: Role/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleVM formRole)
        {
            if (!ModelState.IsValid)
                return View(formRole);

            var role = await _roleManager.FindByIdAsync(formRole.Id);
            if (role == null)
                return NotFound();

            role.Name = formRole.RoleName;

            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(formRole);
        }

        // GET: Role/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var vm = new RoleVM
            {
                Id = role.Id,
                RoleName = role.Name
            };

            return View(vm); // will look for Views/Role/Delete.cshtml
        }


        // POST: Role/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                // return same confirmation page with errors
                var vm = new RoleVM { Id = role.Id, RoleName = role.Name };
                return View("Delete", vm);
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
