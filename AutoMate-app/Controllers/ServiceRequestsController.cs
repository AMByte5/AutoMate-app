using AutoMate_app.Data;
using AutoMate_app.Models;
using AutoMate_app.Models.Options;
using AutoMate_app.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoMate_app.Controllers
{
    [Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly GeminiAdvisorService _geminiAdvisor;
        private readonly GoogleMapsOptions _googleMapsOptions;
        private readonly ILogger<ServiceRequestsController> _logger;
        public ServiceRequestsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, GeminiAdvisorService geminiAdvisor, IOptions<GoogleMapsOptions> mapsOptions)
        {
            _context = context;
            _userManager = userManager;
            _geminiAdvisor = geminiAdvisor;
            _googleMapsOptions = mapsOptions.Value;
        }



        // GET: ServiceRequests
        public async Task<IActionResult> Index(string searchString, int? serviceTypeId, ServiceStatus? status,
            string sortOrder, DateTime? startDate, DateTime? endDate)
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            IQueryable<ServiceRequest> serviceRequests = _context.ServiceRequests
                .Include(s => s.Client)
                .Include(s => s.Mechanic)
                .Include(s => s.ServiceType);

            // Filter based on user role
            if (userRoles.Contains("Admin"))
            {
                // Admins see all requests
                serviceRequests = serviceRequests;
            }
            else if (userRoles.Contains("Mechanic"))
            {
                // Mechanics see requests assigned to them OR unassigned (pending) requests
                serviceRequests = serviceRequests.Where(sr =>
                    sr.MechanicId == currentUserId ||
                    sr.MechanicId == null);
            }
            else
            {
                // Clients see only their own requests
                serviceRequests = serviceRequests.Where(sr => sr.ClientId == currentUserId);
            }

            // Search functionality
            if (!string.IsNullOrEmpty(searchString))
            {
                serviceRequests = serviceRequests.Where(sr =>
                    sr.ProblemDescription.Contains(searchString) ||
                    sr.LocationAddress.Contains(searchString) ||
                    (sr.Client != null && sr.Client.Email.Contains(searchString)) ||
                    (sr.Mechanic != null && sr.Mechanic.Email.Contains(searchString)));
            }

            // Filter by Service Type
            if (serviceTypeId.HasValue && serviceTypeId.Value > 0)
            {
                serviceRequests = serviceRequests.Where(sr => sr.ServiceTypeId == serviceTypeId.Value);
            }

            // Filter by Status
            if (status.HasValue)
            {
                serviceRequests = serviceRequests.Where(sr => sr.Status == status.Value);
            }

            // Filter by Date Range
            if (startDate.HasValue)
            {
                serviceRequests = serviceRequests.Where(sr => sr.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                serviceRequests = serviceRequests.Where(sr => sr.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));
            }

            // Sorting
            ViewBag.CurrentSort = sortOrder;
            ViewBag.DateSortParm = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.StatusSortParm = sortOrder == "Status" ? "status_desc" : "Status";
            ViewBag.ServiceTypeSortParm = sortOrder == "ServiceType" ? "servicetype_desc" : "ServiceType";

            switch (sortOrder)
            {
                case "date_desc":
                    serviceRequests = serviceRequests.OrderByDescending(sr => sr.CreatedAt);
                    break;
                case "Status":
                    serviceRequests = serviceRequests.OrderBy(sr => sr.Status);
                    break;
                case "status_desc":
                    serviceRequests = serviceRequests.OrderByDescending(sr => sr.Status);
                    break;
                case "ServiceType":
                    serviceRequests = serviceRequests.OrderBy(sr => sr.ServiceType.Name);
                    break;
                case "servicetype_desc":
                    serviceRequests = serviceRequests.OrderByDescending(sr => sr.ServiceType.Name);
                    break;
                default:
                    serviceRequests = serviceRequests.OrderByDescending(sr => sr.CreatedAt);
                    break;
            }

            // Populate ViewBag for dropdowns
            ViewBag.ServiceTypeId = new SelectList(_context.ServiceTypes, "Id", "Name", serviceTypeId);
            ViewBag.Status = new SelectList(Enum.GetValues(typeof(ServiceStatus)), status);
            ViewBag.SearchString = searchString;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(await serviceRequests.ToListAsync());
        }

        // GET: ServiceRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Client)
                .Include(s => s.Mechanic)
                .Include(s => s.ServiceType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            // Authorization check
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin"))
            {
                if (userRoles.Contains("Mechanic"))
                {
                    // Mechanics can only see requests assigned to them or unassigned
                    if (serviceRequest.MechanicId != null && serviceRequest.MechanicId != currentUserId)
                    {
                        return Forbid(); // or return NotFound() for security
                    }
                }
                else
                {
                    // Clients can only see their own requests
                    if (serviceRequest.ClientId != currentUserId)
                    {
                        return Forbid();
                    }
                }
            }

            return View(serviceRequest);
        }

        // GET: ServiceRequests/Create
        public IActionResult Create()
        {
            var serviceTypes = _context.ServiceTypes
                .AsNoTracking()
                .Select(s => new { s.Id, s.Name })
                .ToList();

            // Group rules (UI-only)
            var repairMaintenance = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Oil Change & Fluids",
        "Engine & Diagnostics",
        "Battery & Electrical",
        "Brakes",
        "Tires & Wheels",
        "Transmission & Drivetrain",
        "Suspension & Steering",
        "Heating & AC (HVAC)",
        "Exhaust & Emissions"
    };

            var roadside = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Towing & Recovery",
        "Roadside Assistance"
    };

            var washDetailing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Car Wash",
        "Detailing (Interior / Exterior)"
    };

            var grouped = serviceTypes.Select(s => new
            {
                s.Id,
                s.Name,
                Group = repairMaintenance.Contains(s.Name) ? "🔧 Repair / Maintenance"
                      : roadside.Contains(s.Name) ? "🚗 Roadside Assistance"
                      : washDetailing.Contains(s.Name) ? "🧽 Car Wash / Detailing"
                      : "Other"
            })
            .OrderBy(x => x.Group)
            .ThenBy(x => x.Name)
            .ToList();

            ViewBag.GoogleMapsKey = _googleMapsOptions.ApiKey;
            ViewBag.ServiceTypeGrouped = grouped;
            return View();
        }



        // POST: ServiceRequests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceTypeId,ProblemDescription,LocationAddress,LocationLatitude,LocationLongitude")] ServiceRequest serviceRequest)
        {
            // Clear validation errors for fields we set in the controller
            ModelState.Remove(nameof(ServiceRequest.ClientId));
            ModelState.Remove(nameof(ServiceRequest.Client));
            ModelState.Remove(nameof(ServiceRequest.ServiceType));
            ModelState.Remove(nameof(ServiceRequest.Status));
            ModelState.Remove(nameof(ServiceRequest.CreatedAt));
            ModelState.Remove(nameof(ServiceRequest.MechanicId));

            if (ModelState.IsValid)
            {
                // Set fields that aren't in the form
                serviceRequest.ClientId = _userManager.GetUserId(User);
                serviceRequest.Status = ServiceStatus.Pending;
                serviceRequest.CreatedAt = DateTime.UtcNow;

                // Call Gemini
                try
                {
                    var ai = await _geminiAdvisor.GetAdviceAsync(serviceRequest.ProblemDescription);
                    if (ai != null)
                    {
                        serviceRequest.AiSuggestedServiceType = ai.ServiceType;
                        serviceRequest.AiPossibleReasonsJson = JsonSerializer.Serialize(ai.PossibleReasons);
                        serviceRequest.AiUrgency = ai.Urgency;
                        serviceRequest.AiRecommendTowing = ai.RecommendTowing;
                        serviceRequest.AiCalculatedAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {

                }

                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = serviceRequest.Id });
            }

            // Repopulate dropdown if validation fails
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "Id", "Name", serviceRequest.ServiceTypeId);
            return View(serviceRequest);
        }
        // GET: ServiceRequests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && serviceRequest.ClientId != currentUserId)
            {
                return Forbid();
            }
            ViewData["ClientId"] = new SelectList(_context.Users, "Id", "Email", serviceRequest.ClientId);
            ViewData["MechanicId"] = new SelectList(_context.Users, "Id", "Email", serviceRequest.MechanicId);
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "Id", "Name", serviceRequest.ServiceTypeId);
            return View(serviceRequest);
        }

        // POST: ServiceRequests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ClientId,MechanicId,ServiceTypeId,ProblemDescription,LocationAddress,LocationLatitude,LocationLongitude,CreatedAt,Status")] ServiceRequest serviceRequest)
        {

            if (id != serviceRequest.Id)
            {
                return NotFound();
            }
            // Authorization check
            var existingRequest = await _context.ServiceRequests.AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == id);

            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && existingRequest.ClientId != currentUserId)
            {
                return Forbid();
            }


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(serviceRequest.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Users, "Id", "Email", serviceRequest.ClientId);
            ViewData["MechanicId"] = new SelectList(_context.Users, "Id", "Email", serviceRequest.MechanicId);
            ViewData["ServiceTypeId"] = new SelectList(_context.ServiceTypes, "Id", "Name", serviceRequest.ServiceTypeId);
            return View(serviceRequest);
        }

        // GET: ServiceRequests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Client)
                .Include(s => s.Mechanic)
                .Include(s => s.ServiceType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (serviceRequest == null)
            {
                return NotFound();
            }
            // Authorization check
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && serviceRequest.ClientId != currentUserId)
            {
                return Forbid();
            }

            return View(serviceRequest);
        }

        // POST: ServiceRequests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Authorization check
            var existingRequest = await _context.ServiceRequests.AsNoTracking()
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (existingRequest == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && existingRequest.ClientId != currentUserId)
            {
                return Forbid();
            }
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                _context.ServiceRequests.Remove(serviceRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: ServiceRequests/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Mechanic,Admin")]
        public async Task<IActionResult> Accept(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            // Only allow accepting if unassigned or admin
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && serviceRequest.MechanicId != null)
            {
                return Forbid(); // Already assigned
            }

            serviceRequest.MechanicId = currentUserId;
            serviceRequest.Status = ServiceStatus.Accepted;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.Id == id);
        }
    }
}
