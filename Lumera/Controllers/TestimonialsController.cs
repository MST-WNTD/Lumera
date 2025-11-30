using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumera.Data;
using Lumera.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Lumera.Controllers
{
    public class TestimonialsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestimonialsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var testimonials = await GetTestimonialsFromReviews();
            var stats = await GetPlatformStats();
            var videoTestimonials = await GetVideoTestimonials();

            var viewModel = new TestimonialsViewModel
            {
                Testimonials = testimonials,
                VideoTestimonials = videoTestimonials,
                Stats = stats
            };

            return View("~/Views/Home/Testimonials.cshtml", viewModel);
        }

        private async Task<List<TestimonialViewModel>> GetTestimonialsFromReviews()
        {
            // First, get reviews without including Service to avoid BookingCount issue
            var reviews = await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Booking)
                .Where(r => r.IsApproved && r.Rating >= 4)
                .OrderByDescending(r => r.CreatedAt)
                .Take(12)
                .ToListAsync();

            // Then manually load services if needed
            var bookingIds = reviews.Where(r => r.BookingID.HasValue)
                                    .Select(r => r.BookingID.Value)
                                    .Distinct()
                                    .ToList();

            var bookingServices = await _context.Bookings
                .Where(b => bookingIds.Contains(b.BookingID))
                .Select(b => new { b.BookingID, b.ServiceID })
                .ToListAsync();

            var serviceIds = bookingServices.Select(bs => bs.ServiceID).Distinct().ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceID))
                .Select(s => new { s.ServiceID, s.Category })
                .ToListAsync();

            var testimonials = new List<TestimonialViewModel>();

            foreach (var review in reviews)
            {
                string? eventCategory = null;

                if (review.BookingID.HasValue)
                {
                    var bookingService = bookingServices.FirstOrDefault(bs => bs.BookingID == review.BookingID.Value);
                    if (bookingService != null)
                    {
                        var service = services.FirstOrDefault(s => s.ServiceID == bookingService.ServiceID);
                        eventCategory = service?.Category;
                    }
                }

                var testimonial = new TestimonialViewModel
                {
                    ReviewID = review.ReviewID,
                    Content = review.ReviewText ?? GetDefaultTestimonialContent(review.Rating, eventCategory),
                    Rating = review.Rating,
                    AuthorName = $"{review.Reviewer?.FirstName} {review.Reviewer?.LastName}",
                    AuthorRole = GetAuthorRole(eventCategory),
                    AuthorAvatar = review.Reviewer?.AvatarURL ?? GetDefaultAvatar(review.ReviewerID ?? 0),
                    EventType = eventCategory ?? "Event",
                    CreatedAt = review.CreatedAt
                };
                testimonials.Add(testimonial);
            }

            // If we don't have enough reviews, add some placeholder testimonials
            if (testimonials.Count < 6)
            {
                testimonials.AddRange(GetPlaceholderTestimonials(6 - testimonials.Count));
            }

            return testimonials.Take(6).ToList();
        }

        private Task<List<VideoTestimonialViewModel>> GetVideoTestimonials()
        {
            return Task.FromResult(new List<VideoTestimonialViewModel>
            {
                new VideoTestimonialViewModel
                {
                    Title = "Maria & John's Wedding",
                    AuthorName = "Maria & John",
                    AuthorRole = "Wedding Clients",
                    AuthorAvatar = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                    Content = "LUMERA made our dream wedding come true! From finding the perfect venue to coordinating with vendors, everything was seamless."
                },
                new VideoTestimonialViewModel
                {
                    Title = "TechCorp Annual Conference",
                    AuthorName = "James Rodriguez",
                    AuthorRole = "Event Manager, TechCorp",
                    AuthorAvatar = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                    Content = "The AI planner helped us optimize our conference budget and timeline. Our most successful event yet!"
                }
            });
        }

        private async Task<PlatformStatsViewModel> GetPlatformStats()
        {
            var stats = new PlatformStatsViewModel
            {
                EventsPlanned = await _context.Events.CountAsync(e => e.Status == "Completed"),
                ClientSatisfaction = await CalculateSatisfactionRate(),
                TrustedPartners = await _context.Users.CountAsync(u => (u.Role == "Organizer" || u.Role == "Supplier") && u.IsApproved),
                EventCategories = await _context.Services.Select(s => s.Category).Distinct().CountAsync()
            };

            return stats;
        }

        private async Task<int> CalculateSatisfactionRate()
        {
            var totalReviews = await _context.Reviews.CountAsync();
            if (totalReviews == 0) return 98; // Default high satisfaction rate

            var positiveReviews = await _context.Reviews.CountAsync(r => r.Rating >= 4);
            return (int)Math.Round((double)positiveReviews / totalReviews * 100);
        }

        private string GetDefaultTestimonialContent(int rating, string? category)
        {
            var categoryText = category ?? "event";
            return rating switch
            {
                5 => $"Absolutely amazing experience planning our {categoryText} with LUMERA! Everything was perfect and exceeded our expectations.",
                4 => $"Great service for our {categoryText}! LUMERA made the planning process smooth and stress-free.",
                _ => $"Good experience with our {categoryText} planning. LUMERA provided reliable service and good support."
            };
        }

        private string GetAuthorRole(string? category)
        {
            if (string.IsNullOrEmpty(category)) return "Event Client";

            return category switch
            {
                "Wedding" => "Wedding Client",
                "Corporate" or "Conference" => "Corporate Event Planner",
                "Birthday" => "Birthday Party Host",
                _ => "Event Client"
            };
        }

        private string GetDefaultAvatar(int userId)
        {
            var defaultAvatars = new[]
            {
                "https://images.unsplash.com/photo-1494790108755-2616b612b786?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1534528741775-53994a69daeb?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60"
            };

            return defaultAvatars[userId % defaultAvatars.Length];
        }

        private List<TestimonialViewModel> GetPlaceholderTestimonials(int count)
        {
            var placeholders = new List<TestimonialViewModel>
            {
                new TestimonialViewModel
                {
                    Content = "LUMERA made planning our wedding so much easier! The AI planner helped us find the perfect venue and caterer within our budget. Everything was flawless on our big day.",
                    Rating = 5,
                    AuthorName = "Maria Santos",
                    AuthorRole = "Wedding Client",
                    AuthorAvatar = "https://images.unsplash.com/photo-1494790108755-2616b612b786?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60"
                },
                new TestimonialViewModel
                {
                    Content = "Our corporate conference was a huge success thanks to LUMERA. The platform connected us with reliable vendors and the timeline feature kept everything on track.",
                    Rating = 5,
                    AuthorName = "James Rodriguez",
                    AuthorRole = "Corporate Event Planner",
                    AuthorAvatar = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60"
                },
                new TestimonialViewModel
                {
                    Content = "I was overwhelmed planning my daughter's birthday party, but LUMERA's AI assistant guided me through every step. The decorations and entertainment were perfect!",
                    Rating = 5,
                    AuthorName = "Anna Lim",
                    AuthorRole = "Birthday Party Host",
                    AuthorAvatar = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60"
                }
            };

            return placeholders.Take(count).ToList();
        }
    }

    // View Models
    public class TestimonialsViewModel
    {
        public List<TestimonialViewModel> Testimonials { get; set; } = new List<TestimonialViewModel>();
        public List<VideoTestimonialViewModel> VideoTestimonials { get; set; } = new List<VideoTestimonialViewModel>();
        public PlatformStatsViewModel Stats { get; set; } = new PlatformStatsViewModel();
    }

    public class TestimonialViewModel
    {
        public int ReviewID { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class VideoTestimonialViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;
        public string AuthorAvatar { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class PlatformStatsViewModel
    {
        public int EventsPlanned { get; set; }
        public int ClientSatisfaction { get; set; }
        public int TrustedPartners { get; set; }
        public int EventCategories { get; set; }
    }
}