// ServicesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumera.Data;
using Lumera.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Lumera.Controllers
{
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> BrowseServices(string? category = null, string? search = null, int page = 1, int pageSize = 6)
        {
            try
            {
                // Build query for active and approved services
                var query = _context.Services
                    .Include(s => s.Gallery)
                    .Where(s => s.IsActive && s.IsApproved);

                // Apply category filter
                if (!string.IsNullOrEmpty(category) && category != "all")
                {
                    query = query.Where(s => s.Category == category);
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s =>
                        s.ServiceName.Contains(search) ||
                        s.ServiceDescription.Contains(search) ||
                        s.Category.Contains(search));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination and ordering - Load the entities first
                var servicesFromDb = await query
                    .OrderByDescending(s => s.AverageRating)
                    .ThenByDescending(s => s.TotalReviews)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view models after loading from database
                var services = servicesFromDb.Select(s => new ServiceViewModel
                {
                    ServiceID = s.ServiceID,
                    ServiceName = s.ServiceName,
                    ServiceDescription = s.ServiceDescription,
                    Category = s.Category,
                    BasePrice = s.BasePrice,
                    PriceType = s.PriceType,
                    Location = s.Location,
                    AverageRating = s.AverageRating,
                    TotalReviews = s.TotalReviews,
                    ProviderID = s.ProviderID,
                    ProviderType = s.ProviderType,
                 //   ImageURL = s.Gallery.FirstOrDefault()?.ImageURL ?? GetDefaultServiceImage(s.Category)
                }).ToList();

                // Get service categories for filter
                var categories = await _context.Services
                    .Where(s => s.IsActive && s.IsApproved)
                    .Select(s => s.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                var viewModel = new ServiceListViewModel
                {
                    Services = services,
                    Categories = categories,
                    SelectedCategory = category,
                    SearchQuery = search,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };

                return View("~/Views/Home/BrowseServices.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error fetching services: {ex.Message}");

                // Return empty view model on error
                var viewModel = new ServiceListViewModel
                {
                    Services = new List<ServiceViewModel>(),
                    Categories = new List<string> { "Catering", "Photography", "Venue", "Decoration", "Entertainment", "Florist", "Transportation", "Audio Visual" }
                };

                return View("~/Views/Home/BrowseServices.cshtml", viewModel);
            }
        }

        public async Task<IActionResult> ServiceDetails(int id)
        {
            try
            {
                var service = await _context.Services
                    .Include(s => s.Gallery)
                    .FirstOrDefaultAsync(s => s.ServiceID == id && s.IsActive && s.IsApproved);

                if (service == null)
                {
                    return NotFound();
                }

                // Get provider information based on provider type
                object? provider = null;
                if (service.ProviderType == "Organizer")
                {
                    provider = await _context.Organizers
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrganizerID == service.ProviderID);
                }
                else if (service.ProviderType == "Supplier")
                {
                    provider = await _context.Suppliers
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.SupplierID == service.ProviderID);
                }

                // Get reviews for this service
                var reviews = await _context.Reviews
                    .Include(r => r.Reviewer)
                    .Where(r => r.RevieweeID == service.ProviderID && r.RevieweeType == service.ProviderType)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();

                var viewModel = new ServiceDetailViewModel
                {
                    Service = service,
                    Provider = provider,
                    Reviews = reviews,
                    Gallery = service.Gallery.ToList()
                };

                return View("ServiceDetails", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching service details: {ex.Message}");
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> ContactService(int serviceId, string message)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "Please log in to contact service providers." });
                }

                var service = await _context.Services.FindAsync(serviceId);
                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found." });
                }

                // In a real application, you would:
                // 1. Create a conversation between the user and provider
                // 2. Send an initial message
                // 3. Possibly send email notifications

                // For now, we'll just return success
                return Json(new { success = true, message = "Your message has been sent to the service provider." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error contacting service: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while sending your message." });
            }
        }

        private string GetDefaultServiceImage(string category)
        {
            // Map categories to default images
            var imageMap = new Dictionary<string, string>
            {
                { "Catering", "https://images.unsplash.com/photo-1555244162-803834f70033?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Photography", "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Videography", "https://images.unsplash.com/photo-1554048612-b6a482bc67e5?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Venue", "https://images.unsplash.com/photo-1549451371-64aa98a6f660?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Decoration", "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Entertainment", "https://images.unsplash.com/photo-1519677100203-a0e668c92439?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Florist", "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Transportation", "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" },
                { "Audio Visual", "https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60" }
            };

            return imageMap.ContainsKey(category) ? imageMap[category] : "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60";
        }

        private int GetCurrentUserId()
        {
            // Implement your actual user ID retrieval logic here
            // This is a placeholder - replace with your authentication logic
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }
    }

    public class ServiceListViewModel
    {
        public List<ServiceViewModel> Services { get; set; } = new List<ServiceViewModel>();
        public List<string> Categories { get; set; } = new List<string>();
        public string? SelectedCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
    }

    public class ServiceViewModel
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal? BasePrice { get; set; }
        public string? PriceType { get; set; }
        public string? Location { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int ProviderID { get; set; }
        public string ProviderType { get; set; } = string.Empty;
        public string? ImageURL { get; set; }
        public string? Description { get; internal set; }
        public decimal Price { get; internal set; }
        public int Rating { get; internal set; }
        public int ReviewCount { get; internal set; }
    }

    public class ServiceDetailViewModel
    {
        public Client Client { get; set; } = null!;
        public Service Service { get; set; } = null!;
        public object? Provider { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<ServiceGallery> Gallery { get; set; } = new List<ServiceGallery>();
        public int UnreadMessages { get; set; }
    }
}