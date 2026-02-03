using AutoMate_app.Models;
using Microsoft.AspNetCore.Http;
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

        // GET: RoleController/Details/5
        public ActionResult Details(string id)
        {
            return View();
        }

        // GET: RoleController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RoleController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoleVM formRole)
        {
            try
            {
                IdentityRole role = new IdentityRole(formRole.RoleName);
                var result = _roleManager.CreateAsync(role).Result;
                if (result.Succeeded)
                    return RedirectToAction(nameof(Index));
                else
                    return View();
            }
            catch
            {
                return View();
            }
        }

        // GET: RoleController/Edit/5
        public ActionResult Edit(string id)
        {
            return View();
        }

        // POST: RoleController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(String id, IFormCollection collection)
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

        // GET: RoleController/Delete/5
        public ActionResult Delete(string id)
        {
            return View();
        }

        // POST: RoleController/Delete/5
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
