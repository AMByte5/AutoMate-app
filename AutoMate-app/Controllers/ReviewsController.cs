using System;
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
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            IQueryable<Review> reviews = _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.Mechanic)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.ServiceType);

            // Filter based on user role
            if (userRoles.Contains("Admin"))
            {
                // Admins see all reviews
                reviews = reviews;
            }
            else if (userRoles.Contains("Mechanic"))
            {
                // Mechanics see reviews for their service requests
                reviews = reviews.Where(r => r.ServiceRequest.MechanicId == currentUserId);
            }
            else
            {
                // Clients see only their own reviews
                reviews = reviews.Where(r => r.ClientId == currentUserId);
            }

            return View(await reviews.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        // GET: Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.Mechanic)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.ServiceType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null)
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
                    // Mechanics can only see reviews for their service requests
                    if (review.ServiceRequest.MechanicId != currentUserId)
                    {
                        return Forbid();
                    }
                }
                else
                {
                    // Clients can only see their own reviews
                    if (review.ClientId != currentUserId)
                    {
                        return Forbid();
                    }
                }
            }

            return View(review);
        }

        // GET: Reviews/Create
        public async Task<IActionResult> Create(int? serviceRequestId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // If serviceRequestId is provided, validate it
            if (serviceRequestId.HasValue)
            {
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Mechanic)
                    .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId.Value);

                if (serviceRequest == null)
                {
                    return NotFound();
                }

                // Authorization: Only the client who owns the request can review
                if (serviceRequest.ClientId != currentUserId)
                {
                    return Forbid();
                }

                // Check if service is completed
                if (serviceRequest.Status != ServiceStatus.Completed)
                {
                    TempData["Error"] = "You can only review completed service requests.";
                    return RedirectToAction("Details", "ServiceRequests", new { id = serviceRequestId });
                }

                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ServiceRequestId == serviceRequestId.Value);

                if (existingReview != null)
                {
                    TempData["Error"] = "You have already reviewed this service request.";
                    return RedirectToAction("Details", "ServiceRequests", new { id = serviceRequestId });
                }

                // Check if mechanic exists
                if (string.IsNullOrEmpty(serviceRequest.MechanicId))
                {
                    TempData["Error"] = "Cannot review a service request without an assigned mechanic.";
                    return RedirectToAction("Details", "ServiceRequests", new { id = serviceRequestId });
                }

                ViewData["ServiceRequestId"] = serviceRequestId.Value;
                ViewData["ServiceRequestInfo"] = $"Service Request #{serviceRequest.Id} - {serviceRequest.ServiceType?.Name ?? "Service"}";
            }
            else
            {
                // Get completed service requests for current user that don't have reviews
                var completedRequests = await _context.ServiceRequests
                    .Where(sr => sr.ClientId == currentUserId
                        && sr.Status == ServiceStatus.Completed
                        && sr.MechanicId != null
                        && !_context.Reviews.Any(r => r.ServiceRequestId == sr.Id))
                    .Include(sr => sr.ServiceType)
                    .ToListAsync();

                ViewData["ServiceRequestId"] = new SelectList(completedRequests, "Id", "Id");
            }

            return View();
        }

        // POST: Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceRequestId,Rating,Comment")] Review review)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                // Validate service request
                var serviceRequest = await _context.ServiceRequests
                    .Include(sr => sr.Mechanic)
                    .FirstOrDefaultAsync(sr => sr.Id == review.ServiceRequestId);

                if (serviceRequest == null)
                {
                    return NotFound();
                }

                // Authorization checks
                if (serviceRequest.ClientId != currentUserId)
                {
                    return Forbid();
                }

                if (serviceRequest.Status != ServiceStatus.Completed)
                {
                    ModelState.AddModelError("", "You can only review completed service requests.");
                    ViewData["ServiceRequestId"] = review.ServiceRequestId;
                    return View(review);
                }

                // Check for duplicate review
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ServiceRequestId == review.ServiceRequestId);

                if (existingReview != null)
                {
                    ModelState.AddModelError("", "You have already reviewed this service request.");
                    ViewData["ServiceRequestId"] = review.ServiceRequestId;
                    return View(review);
                }

                // Check if mechanic exists
                if (string.IsNullOrEmpty(serviceRequest.MechanicId))
                {
                    ModelState.AddModelError("", "Cannot review a service request without an assigned mechanic.");
                    ViewData["ServiceRequestId"] = review.ServiceRequestId;
                    return View(review);
                }

                // Set review properties
                review.ClientId = currentUserId;
                review.CreatedAt = DateTime.UtcNow;

                // Add review
                _context.Add(review);
                await _context.SaveChangesAsync();

                // Update MechanicProfile ratings
                await UpdateMechanicRating(serviceRequest.MechanicId);

                TempData["Success"] = "Review submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["ServiceRequestId"] = review.ServiceRequestId;
            return View(review);
        }

        // GET: Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.ServiceRequest)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Authorization: Only the client who wrote the review can edit
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && review.ClientId != currentUserId)
            {
                return Forbid();
            }

            ViewData["ServiceRequestId"] = review.ServiceRequestId;
            return View(review);
        }

        // POST: Reviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ServiceRequestId,ClientId,Rating,Comment,CreatedAt")] Review review)
        {
            if (id != review.Id)
            {
                return NotFound();
            }

            // Authorization check
            var existingReview = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.ServiceRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReview == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && existingReview.ClientId != currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(review);
                    await _context.SaveChangesAsync();

                    // Update mechanic rating if rating changed
                    if (existingReview.Rating != review.Rating && existingReview.ServiceRequest.MechanicId != null)
                    {
                        await UpdateMechanicRating(existingReview.ServiceRequest.MechanicId);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReviewExists(review.Id))
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

            ViewData["ServiceRequestId"] = review.ServiceRequestId;
            return View(review);
        }

        // GET: Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.Mechanic)
                .Include(r => r.ServiceRequest)
                    .ThenInclude(sr => sr.ServiceType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Authorization: Only the client who wrote the review or admin can delete
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && review.ClientId != currentUserId)
            {
                return Forbid();
            }

            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews
                .Include(r => r.ServiceRequest)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            // Authorization check
            var currentUserId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Contains("Admin") && review.ClientId != currentUserId)
            {
                return Forbid();
            }

            var mechanicId = review.ServiceRequest?.MechanicId;

            // Delete review
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            // Update mechanic rating after deletion
            if (!string.IsNullOrEmpty(mechanicId))
            {
                await UpdateMechanicRating(mechanicId);
            }

            TempData["Success"] = "Review deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }

        // Helper method to update mechanic's average rating
        private async Task UpdateMechanicRating(string mechanicUserId)
        {
            if (string.IsNullOrEmpty(mechanicUserId))
                return;

            // Get all reviews for service requests assigned to this mechanic
            var reviews = await _context.Reviews
                .Where(r => r.ServiceRequest.MechanicId == mechanicUserId)
                .Select(r => r.Rating)
                .ToListAsync();

            // Get or create mechanic profile
            var mechanicProfile = await _context.MechanicProfiles
                .FirstOrDefaultAsync(mp => mp.UserId == mechanicUserId);

            if (mechanicProfile == null)
            {
                // If no profile exists, we can't update rating
                // You might want to create one or handle this case differently
                return;
            }

            if (reviews.Any())
            {
                mechanicProfile.AverageRating = reviews.Average();
                mechanicProfile.TotalReviews = reviews.Count;
            }
            else
            {
                // No reviews, reset to 0
                mechanicProfile.AverageRating = 0;
                mechanicProfile.TotalReviews = 0;
            }

            _context.Update(mechanicProfile);
            await _context.SaveChangesAsync();
        }
    }
}