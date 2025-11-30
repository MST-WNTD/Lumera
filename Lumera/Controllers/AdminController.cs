using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Lumera.Data;
using Lumera.Models;
using Lumera.Models.AdminViewModels;
using System.Security.Claims;

namespace Lumera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {

            var viewModel = new AdminDashboardViewModel
            {
                Users = await GetUsersAsync(),
                PendingContent = await GetPendingContentAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role != "Admin"),
                PendingApprovals = await _context.Users.CountAsync(u => u.IsActive && !u.IsApproved),
                ActiveEvents = await _context.Events
                    .CountAsync(e => e.Status == "Planning" || e.Status == "Confirmed" || e.Status == "In Progress"),
                TotalRevenue = await _context.Transactions
                    .Where(t => t.Status == "Completed")
                    .SumAsync(t => (decimal?)t.Amount) ?? 0
            };

            return View(viewModel);
        }

        private async Task<List<AdminUserViewModel>> GetUsersAsync()
        {
            return await _context.Users
                .Where(u => u.Role != "Admin")
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserViewModel
                {
                    UserID = u.UserID,
                    Email = u.Email,
                    Role = u.Role,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarURL = u.AvatarURL,
                    IsActive = u.IsActive,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();
        }

        private async Task<List<ContentModerationItem>> GetPendingContentAsync()
        {
            var pendingServices = await _context.Services
                .Where(s => !s.IsApproved && s.IsActive)
                .Select(s => new ContentModerationItem
                {
                    ItemID = s.ServiceID,
                    Name = s.ServiceName,
                    ProviderName = s.ProviderType == "Organizer"
                        ? _context.Organizers.Where(o => o.UserID == s.ProviderID).Select(o => o.BusinessName).FirstOrDefault() ?? "Unknown"
                        : _context.Suppliers.Where(sp => sp.UserID == s.ProviderID).Select(sp => sp.BusinessName).FirstOrDefault() ?? "Unknown",
                    Category = s.Category,
                    Type = "Service",
                    Status = "Pending",
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return pendingServices;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null) return BadRequest("User not found");

            switch (request.Action.ToLower())
            {
                case "approve":
                    user.IsApproved = true;
                    break;
                case "suspend":
                    user.IsActive = false;
                    break;
                case "activate":
                    user.IsActive = true;
                    break;
                case "delete":
                    user.IsActive = false;
                    break;
                default:
                    return BadRequest("Invalid action");
            }

            await _context.SaveChangesAsync();
            LogAdminAction($"Updated user {request.UserId} - {request.Action}");

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModerateContent([FromBody] ModerateContentRequest request)
        {
            if (request.Type == "Service")
            {
                var service = await _context.Services.FindAsync(request.Id);
                if (service != null)
                {
                    service.IsApproved = request.Action == "approve";
                    if (request.Action == "reject")
                    {
                        service.IsActive = false;
                    }
                }
            }

            await _context.SaveChangesAsync();
            LogAdminAction($"Moderated {request.Type} {request.Id} - {request.Action}");

            return Ok();
        }

        private void LogAdminAction(string description)
        {
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim != null && int.TryParse(adminIdClaim.Value, out int adminId))
            {
                _logger.LogInformation($"Admin Action - AdminID: {adminId}, Description: {description}, Date: {DateTime.Now}");
            }
        }

        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (user == null) return NotFound();

            return View(user);
        }

        public async Task<IActionResult> ContentDetails(int id, string type)
        {
            if (type == "Service")
            {
                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == id);
                return View(service);
            }
            else
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ReviewID == id);
                return View(review);
            }
        }

        public async Task<IActionResult> UserManagement(int page = 1, int pageSize = 10)
        {
            var usersQuery = _context.Users.AsQueryable();

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserViewModel
                {
                    UserID = u.UserID,
                    Email = u.Email,
                    Role = u.Role,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarURL = u.AvatarURL,
                    IsActive = u.IsActive,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    StartIndex = ((page - 1) * pageSize) + 1,
                    EndIndex = Math.Min(page * pageSize, totalCount)
                }
            };

            return View(viewModel);
        }

        public async Task<IActionResult> PendingUsers(int page = 1, int pageSize = 10)
        {
            var usersQuery = _context.Users.Where(u => u.IsActive && !u.IsApproved);

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserViewModel
                {
                    UserID = u.UserID,
                    Email = u.Email,
                    Role = u.Role,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarURL = u.AvatarURL,
                    IsActive = u.IsActive,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    StartIndex = ((page - 1) * pageSize) + 1,
                    EndIndex = Math.Min(page * pageSize, totalCount)
                }
            };

            return View("UserManagement", viewModel);
        }

        public async Task<IActionResult> SuspendedUsers(int page = 1, int pageSize = 10)
        {
            var usersQuery = _context.Users.Where(u => !u.IsActive);

            var totalCount = await usersQuery.CountAsync();
            var users = await usersQuery
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserViewModel
                {
                    UserID = u.UserID,
                    Email = u.Email,
                    Role = u.Role,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AvatarURL = u.AvatarURL,
                    IsActive = u.IsActive,
                    IsApproved = u.IsApproved,
                    CreatedAt = u.CreatedAt,
                    LastLogin = u.LastLogin
                })
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    StartIndex = ((page - 1) * pageSize) + 1,
                    EndIndex = Math.Min(page * pageSize, totalCount)
                }
            };

            return View("UserManagement", viewModel);
        }

        public async Task<IActionResult> ContentModeration()
        {
            var viewModel = new ContentModerationViewModel
            {
                PendingServices = await GetPendingServicesAsync(),
                PendingReviews = await GetAllReviewsAsync(),  // Shows ALL reviews now
                PendingServicesCount = await _context.Services.CountAsync(s => !s.IsApproved && s.IsActive),
                FlaggedReviewsCount = await _context.Reviews.CountAsync(),
                ReportedUsersCount = await _context.Users.CountAsync(u => !u.IsActive || !u.IsApproved),
                PendingEventsCount = await _context.Events.CountAsync(e => e.Status == "Draft" || e.Status == "Planning"),
                ServiceCategories = await _context.Services
                    .Where(s => s.IsActive)
                    .Select(s => s.Category)
                    .Distinct()
                    .ToListAsync()
            };

            return View(viewModel);
        }

        private async Task<List<PendingServiceViewModel>> GetPendingServicesAsync()
        {
            var pendingServices = await _context.Services
                .Where(s => !s.IsApproved && s.IsActive)
                .Select(s => new PendingServiceViewModel
                {
                    ServiceID = s.ServiceID,
                    ServiceName = s.ServiceName,
                    Category = s.Category,
                    CreatedAt = s.CreatedAt,
                    ProviderType = s.ProviderType,
                    ProviderName = s.ProviderType == "Organizer"
                        ? _context.Organizers.Where(o => o.UserID == s.ProviderID).Select(o => o.BusinessName).FirstOrDefault() ?? "Unknown"
                        : _context.Suppliers.Where(sp => sp.UserID == s.ProviderID).Select(sp => sp.BusinessName).FirstOrDefault() ?? "Unknown"
                })
                .ToListAsync();

            return pendingServices;
        }

        private async Task<List<PendingReviewViewModel>> GetAllReviewsAsync()
        {
            try
            {
                _logger.LogInformation("📊 Admin: Loading all reviews");

                // ✅ Get ALL reviews (since they're all auto-approved)
                var allReviews = await _context.Reviews
                    .Where(r => r.BookingID.HasValue)  // ⭐ REMOVED && r.IsApproved
                    .ToListAsync();

                _logger.LogInformation($"Found {allReviews.Count} reviews");

                var reviewViewModels = new List<PendingReviewViewModel>();

                // ✅ Load related data separately
                foreach (var review in allReviews)
                {
                    // Get reviewer
                    var reviewer = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserID == review.ReviewerID);

                    if (reviewer == null) continue;

                    // Get booking
                    var booking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.BookingID == review.BookingID.Value);

                    if (booking == null) continue;

                    // Get service
                    var service = await _context.Services
                        .FirstOrDefaultAsync(s => s.ServiceID == booking.ServiceID);

                    if (service == null) continue;

                    // ✅ Add to results with approval status indicator
                    reviewViewModels.Add(new PendingReviewViewModel
                    {
                        ReviewID = review.ReviewID,
                        ReviewerName = $"{reviewer.FirstName} {reviewer.LastName}",
                        ServiceName = service.ServiceName,
                        Rating = review.Rating,
                        ReviewText = review.ReviewText ?? string.Empty,
                        IsFlagged = false,
                        CreatedAt = review.CreatedAt,
                        // ⭐ Add status (though all should be approved)
                        Status = review.IsApproved ? "Approved" : "Pending"
                    });
                }

                _logger.LogInformation($"✅ Admin: Returning {reviewViewModels.Count} reviews");
                return reviewViewModels.OrderByDescending(r => r.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting all reviews");
                return new List<PendingReviewViewModel>();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModerateService([FromBody] ModerateServiceRequest request)
        {
            var service = await _context.Services.FindAsync(request.ServiceId);
            if (service == null)
                return NotFound("Service not found");

            switch (request.Action.ToLower())
            {
                case "approve":
                    service.IsApproved = true;
                    LogAdminAction($"Approved service: {service.ServiceName}");
                    break;
                case "reject":
                    service.IsActive = false;
                    LogAdminAction($"Rejected service: {service.ServiceName}");
                    break;
                default:
                    return BadRequest("Invalid action");
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModerateReview([FromBody] ModerateReviewRequest request)
        {
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.ReviewID == request.ReviewId);

                if (review == null)
                    return NotFound(new { success = false, message = "Review not found" });

                // Admin can ONLY delete reviews (since all reviews are auto-approved)
                if (request.Action.ToLower() == "remove" ||
                    request.Action.ToLower() == "delete" ||
                    request.Action.ToLower() == "reject")
                {
                    var providerId = review.Booking.ProviderID;
                    var providerType = review.Booking.ProviderType;

                    _context.Reviews.Remove(review);
                    LogAdminAction($"Deleted review ID: {review.ReviewID}");

                    await _context.SaveChangesAsync();

                    // Update provider rating after deletion
                    await UpdateProviderRatingAsync(providerId, providerType);

                    return Ok(new { success = true, message = "Review deleted successfully" });
                }

                return BadRequest(new { success = false, message = "Invalid action. Only 'delete' is allowed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating review");
                return StatusCode(500, new { success = false, message = "Error moderating review" });
            }
        }
        private async Task UpdateProviderRatingAsync(int providerId, string providerType)
        {
            try
            {
                // Only include approved reviews in rating calculation
                var reviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .Where(r => r.Booking.ProviderID == providerId &&
                               r.Booking.ProviderType == providerType &&
                               r.IsApproved)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    // No reviews left - reset ratings to 0
                    if (providerType == "Organizer")
                    {
                        var organizer = await _context.Organizers.FindAsync(providerId);
                        if (organizer != null)
                        {
                            organizer.AverageRating = 0;
                            organizer.TotalReviews = 0;
                        }
                    }
                    else if (providerType == "Supplier")
                    {
                        var supplier = await _context.Suppliers.FindAsync(providerId);
                        if (supplier != null)
                        {
                            supplier.AverageRating = 0;
                            supplier.TotalReviews = 0;
                        }
                    }
                }
                else
                {
                    // Calculate new ratings
                    var avgRating = reviews.Average(r => r.Rating);
                    var totalReviews = reviews.Count;

                    if (providerType == "Organizer")
                    {
                        var organizer = await _context.Organizers.FindAsync(providerId);
                        if (organizer != null)
                        {
                            organizer.AverageRating = (decimal)avgRating;
                            organizer.TotalReviews = totalReviews;
                        }
                    }
                    else if (providerType == "Supplier")
                    {
                        var supplier = await _context.Suppliers.FindAsync(providerId);
                        if (supplier != null)
                        {
                            supplier.AverageRating = (decimal)avgRating;
                            supplier.TotalReviews = totalReviews;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Updated {providerType} {providerId} rating successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error updating provider rating for {providerType} {providerId}");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/fix-organizer-ratings")]
        public async Task<IActionResult> FixOrganizerRatings()
        {
            try
            {
                var organizers = await _context.Organizers.ToListAsync();
                var updated = 0;

                foreach (var organizer in organizers)
                {
                    // Get all approved reviews for this organizer
                    var reviews = await _context.Reviews
                        .Include(r => r.Booking)
                        .Where(r => r.Booking.ProviderID == organizer.OrganizerID &&
                                   r.Booking.ProviderType == "Organizer" &&
                                   r.IsApproved)
                        .ToListAsync();

                    if (reviews.Any())
                    {
                        organizer.AverageRating = (decimal)reviews.Average(r => r.Rating);
                        organizer.TotalReviews = reviews.Count;
                        updated++;
                        _logger.LogInformation($"Updated Organizer {organizer.OrganizerID}: {organizer.AverageRating:F2} ({organizer.TotalReviews} reviews)");
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Updated {updated} organizers",
                    details = organizers.Select(o => new
                    {
                        id = o.OrganizerID,
                        name = o.BusinessName,
                        rating = o.AverageRating,
                        total = o.TotalReviews
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing ratings");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}