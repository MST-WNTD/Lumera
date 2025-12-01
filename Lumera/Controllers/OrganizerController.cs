using Lumera.Data;
using Lumera.Models;
using Lumera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Lumera.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrganizerController> _logger;

        public OrganizerController(ApplicationDbContext context, INotificationService notificationService, ILogger<OrganizerController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> FindOrganizers(string? search, string? specialty, int page = 1, int pageSize = 6)
        {
            try
            {
                var query = _context.Organizers
                    .Include(o => o.User)
                    .Where(o => o.IsActive && o.User != null && o.User.IsApproved);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o =>
                        o.BusinessName.Contains(search) ||
                        o.BusinessDescription.Contains(search) ||
                        o.ServiceAreas.Contains(search));
                }

                // Apply specialty filter
                if (!string.IsNullOrEmpty(specialty) && specialty != "All")
                {
                    query = query.Where(o =>
                        o.ServiceAreas.Contains(specialty) ||
                        o.BusinessDescription.Contains(specialty));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply pagination
                var organizers = await query
                    .OrderByDescending(o => o.AverageRating)
                    .ThenByDescending(o => o.TotalReviews)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrganizerViewModel
                    {
                        OrganizerID = o.OrganizerID,
                        BusinessName = o.BusinessName,
                        BusinessDescription = o.BusinessDescription,
                        AverageRating = o.AverageRating,
                        TotalReviews = o.TotalReviews,
                        YearsOfExperience = o.YearsOfExperience,
                        ServiceAreas = o.ServiceAreas,
                        // You might want to add logic to extract location from ServiceAreas
                        // or create a separate location field in the Organizer model
                        Location = ExtractLocationFromServiceAreas(o.ServiceAreas),
                        Specialty = ExtractPrimarySpecialty(o.ServiceAreas, o.BusinessDescription),
                        ImageURL = GetOrganizerImageUrl(o.OrganizerID) // Implement this based on your image storage
                    })
                    .ToListAsync();

                // Get available specialties for filter
                var specialties = await _context.Organizers
                    .Where(o => o.IsActive)
                    .SelectMany(o => ExtractSpecialtiesFromServiceAreas(o.ServiceAreas))
                    .Distinct()
                    .ToListAsync();

                var viewModel = new OrganizerListViewModel
                {
                    Organizers = organizers,
                    SearchQuery = search,
                    SelectedSpecialty = specialty,
                    Specialties = specialties,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalCount = totalCount
                };

                return View("~/Views/Home/FindOrganizers.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error fetching organizers: {ex.Message}");

                // Return empty view model on error
                var viewModel = new OrganizerListViewModel
                {
                    Organizers = new List<OrganizerViewModel>(),
                    Specialties = new List<string> { "Weddings", "Corporate Events", "Birthdays", "Conferences", "Parties", "Fundraisers" }
                };

                return View("~/Views/Home/FindOrganizers.cshtml", viewModel);
            }
        }

        // Helper methods
        private static string? ExtractLocationFromServiceAreas(string? serviceAreas)
        {
            if (string.IsNullOrEmpty(serviceAreas))
                return "Metro Manila";

            // Simple extraction - you might want to implement more sophisticated logic
            var locations = new[] { "Quezon City", "Makati", "Taguig", "Pasig", "Mandaluyong", "Manila City" };
            foreach (var location in locations)
            {
                if (serviceAreas.Contains(location))
                    return location;
            }

            return "Metro Manila";
        }

        private static string? ExtractPrimarySpecialty(string? serviceAreas, string? businessDescription)
        {
            if (!string.IsNullOrEmpty(serviceAreas))
            {
                var specialties = new[] { "Weddings", "Corporate Events", "Birthdays", "Conferences", "Parties", "Fundraisers" };
                foreach (var specialty in specialties)
                {
                    if (serviceAreas.Contains(specialty))
                        return specialty;
                }
            }

            if (!string.IsNullOrEmpty(businessDescription))
            {
                var specialties = new[] { "Weddings", "Corporate", "Birthdays", "Conferences", "Parties", "Fundraisers" };
                foreach (var specialty in specialties)
                {
                    if (businessDescription.Contains(specialty))
                        return specialty;
                }
            }

            return "Event Planning";
        }

        private static List<string> ExtractSpecialtiesFromServiceAreas(string? serviceAreas)
        {
            var specialties = new List<string> { "All", "Weddings", "Corporate Events", "Birthdays", "Conferences", "Parties", "Fundraisers" };

            if (string.IsNullOrEmpty(serviceAreas))
                return specialties;

            var foundSpecialties = new List<string>();
            foreach (var specialty in specialties.Where(s => s != "All"))
            {
                if (serviceAreas.Contains(specialty))
                    foundSpecialties.Add(specialty);
            }

            return foundSpecialties.Count != 0 ? foundSpecialties : specialties;
        }

        private static string? GetOrganizerImageUrl(int organizerId)
        {
            // Implement logic to get organizer image URL
            // This could be from a gallery table, user avatar, or default images
            // For now, return a placeholder based on ID
            var defaultImages = new[]
            {
                "https://images.unsplash.com/photo-1551833996-2c1d78a13b4f?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1511795409834-ef04bbd61622?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1540575467063-178a50c2df87?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1464366400600-7168b8af9bc3?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1532634922-8fe0b757fb13?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60",
                "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?ixlib=rb-1.2.1&auto=format&fit=crop&w=500&q=60"
            };

            return defaultImages[organizerId % defaultImages.Length];
        }

        // In OrganizerController.cs - Replace your existing Dashboard method with this:

        [HttpGet]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                Console.WriteLine($"Dashboard accessed by User ID: {userId}");

                if (userId == 0)
                {
                    Console.WriteLine("Redirecting to login - userId is 0");
                    return RedirectToAction("LoginSignup", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    Console.WriteLine($"No organizer found for UserID: {userId}");
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && user.Role == "Organizer")
                    {
                        return Content("Organizer profile not found. Please contact administrator.");
                    }
                    return RedirectToAction("LoginSignup", "Account");
                }

                Console.WriteLine($"Loading dashboard for organizer: {organizer.BusinessName}");

                // ADD THIS: Get unread notifications count
                var unreadNotificationsCount = await _notificationService.GetUnreadCountAsync(userId);

                var viewModel = new OrganizerDashboardViewModel
                {
                    Organizer = organizer,
                    ActiveEvents = await GetActiveEventsCount(organizer.OrganizerID),
                    PendingBookings = await GetPendingBookingsCount(organizer.OrganizerID),
                    MonthlyEarnings = await CalculateTotalEarnings(organizer.OrganizerID), // ← Changed this
                    UnreadMessages = await GetUnreadMessagesCount(userId),
                    UnreadNotifications = unreadNotificationsCount,
                    UpcomingEvents = await GetUpcomingEvents(organizer.OrganizerID),
                    RecentBookings = await GetRecentBookings(organizer.OrganizerID),
                    RecentMessages = await GetRecentMessages(userId)
                };

                return View("Dashboard", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer dashboard: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Content($"Error loading dashboard: {ex.Message}");
            }
        }

        // Helper methods for dashboard data
        private async Task<int> GetActiveEventsCount(int organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerID == organizerId &&
                           e.Status != "Completed" &&
                           e.Status != "Cancelled" &&
                           e.EventDate >= DateTime.Today)
                .CountAsync();
        }

        private async Task<int> GetPendingBookingsCount(int organizerId)
        {
            return await _context.Bookings
                .Where(b => b.ProviderID == organizerId &&
                           b.ProviderType == "Organizer" &&
                           b.Status == "Pending")
                .CountAsync();
        }

        private async Task<int> GetUnreadMessagesCount(int userId)
        {
            var conversationIds = await _context.ConversationParticipants
                .Where(cp => cp.UserID == userId)
                .Select(cp => cp.ConversationID)
                .ToListAsync();

            return await _context.Messages
                .Where(m => conversationIds.Contains(m.ConversationID) &&
                           m.SenderID != userId &&
                           !m.IsRead)
                .CountAsync();
        }

        private async Task<List<Event>> GetUpcomingEvents(int organizerId)
        {
            return await _context.Events
                .Where(e => e.OrganizerID == organizerId &&
                           e.EventDate >= DateTime.Today)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<Booking>> GetRecentBookings(int organizerId)
        {
            return await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.Client)
                    .ThenInclude(c => c.User)
                .Where(b => b.ProviderID == organizerId &&
                           b.ProviderType == "Organizer")
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .AsNoTracking()  // Add this to avoid tracking issues
                .ToListAsync();
        }

        private async Task<int> GetTotalBookingsCount(int organizerId)
        {
            return await _context.Bookings
                .Where(b => b.ProviderID == organizerId &&
                           b.ProviderType == "Organizer")
                .CountAsync();
        }
        private async Task<List<Message>> GetRecentMessages(int userId)
        {
            var conversationIds = await _context.ConversationParticipants
                .Where(cp => cp.UserID == userId)
                .Select(cp => cp.ConversationID)
                .ToListAsync();

            return await _context.Messages
                .Include(m => m.Sender)
                .Where(m => conversationIds.Contains(m.ConversationID))
                .OrderByDescending(m => m.SentAt)
                .Take(5)
                .ToListAsync();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> Events()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                // UPDATED: Load ALL events (not just upcoming) and include Bookings
                var events = await _context.Events
                    .Include(e => e.Client)
                        .ThenInclude(c => c.User)
                    .Include(e => e.Bookings)  // Include bookings if needed
                    .Where(e => e.OrganizerID == organizer.OrganizerID)
                    .OrderByDescending(e => e.EventDate)  // Most recent first
                    .ToListAsync();

                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new OrganizerEventsViewModel
                {
                    Organizer = organizer,
                    Events = events,
                    UnreadMessages = unreadMessages
                };

                return View("MyEvents", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer events: {ex.Message}");
                return View("Error");
            }
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            // Return the view for creating a new event
            return View("CreateEvent");
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] Event newEvent)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                // Set the organizer ID and ensure required fields
                newEvent.OrganizerID = organizer.OrganizerID;
                newEvent.CreatedAt = DateTime.Now;
                newEvent.UpdatedAt = DateTime.Now;

                // Validate required fields
                if (string.IsNullOrEmpty(newEvent.EventName))
                {
                    return Json(new { success = false, message = "Event name is required" });
                }

                if (string.IsNullOrEmpty(newEvent.EventType))
                {
                    return Json(new { success = false, message = "Event type is required" });
                }

                if (newEvent.EventDate == default)
                {
                    return Json(new { success = false, message = "Event date is required" });
                }

                // Add the new event to database
                _context.Events.Add(newEvent);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Event created successfully", eventId = newEvent.EventID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while creating the event" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrganizerInfo()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false });
                }

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false });
                }

                return Json(new { success = true, businessName = organizer.BusinessName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting organizer info: {ex.Message}");
                return Json(new { success = false });
            }
        }

        [HttpGet("organizer/events/edit/{id}")]
        public async Task<IActionResult> EditEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .Include(o => o.User)  // ✅ Make sure this is included
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                var eventItem = await _context.Events
                    .Include(e => e.Client)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizer.OrganizerID);

                if (eventItem == null)
                {
                    return NotFound("Event not found");
                }

                // ✅ PREVENT EDITING OF COMPLETED OR CANCELLED EVENTS
                if (eventItem.Status == "Completed" || eventItem.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = "This event cannot be edited because it is " + eventItem.Status.ToLower() + ".";
                    return RedirectToAction("EventDetails", new { id = eventItem.EventID });
                }

                // ✅ ADD THIS LINE - Pass organizer name to view
                ViewBag.OrganizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                // Pass available status options to the view
                ViewBag.StatusOptions = new List<string>
        {
            "Pending",
            "Confirmed",
            "Cancelled",
            "Completed"
        };

                return View("EditEvent", eventItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading event for editing: {ex.Message}");
                return View("Error");
            }
        }

        [HttpPost("organizer/events/edit/{id}")]
        public async Task<IActionResult> UpdateEventStatus(int id, [FromBody] UpdateEventStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .Include(o => o.User)  // ADD THIS - Include User for organizer name
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                var eventItem = await _context.Events
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.Client)      // ADD THIS
                            .ThenInclude(c => c.User)    // ADD THIS
                    .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizer.OrganizerID);

                if (eventItem == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // Update event status
                eventItem.Status = request.Status;
                eventItem.UpdatedAt = DateTime.Now;

                // Update all related bookings when event status changes
                if (eventItem.Bookings != null && eventItem.Bookings.Any())
                {
                    var organizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                    foreach (var booking in eventItem.Bookings)
                    {
                        // Map event status to booking status
                        string newStatus = request.Status switch
                        {
                            "Confirmed" => "Confirmed",
                            "Cancelled" => "Cancelled",
                            "Pending" => "Pending",
                            "Completed" => "Completed",
                            _ => booking.Status
                        };

                        booking.Status = newStatus;

                        // ========== ADD NOTIFICATION FOR EACH BOOKING ==========
                        if (booking.Client?.UserID != null)
                        {
                            await _notificationService.CreateClientBookingStatusNotificationAsync(
                                clientUserId: (int)booking.Client.UserID,
                                bookingId: booking.BookingID,
                                status: newStatus,
                                organizerName: organizerName
                            );

                            Console.WriteLine($"Notification sent to Client UserID: {booking.Client.UserID} for Booking {booking.BookingID}");
                        }
                        // ========== END NOTIFICATION ==========

                        Console.WriteLine($"Booking {booking.BookingID} status updated to {booking.Status}");
                    }
                }

                await _context.SaveChangesAsync();

                Console.WriteLine($"Event {id} status updated to {request.Status}");

                return Json(new { success = true, message = "Event status updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                return Json(new { success = false, message = "Error updating event" });
            }
        }

        [HttpGet("organizer/events/details/{id}")]
        public async Task<IActionResult> EventDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .Include(o => o.User)  // ✅ ADD THIS - Include User for organizer name
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                var eventItem = await _context.Events
                    .Include(e => e.Client)
                        .ThenInclude(c => c.User)
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizer.OrganizerID);

                if (eventItem == null)
                {
                    return NotFound("Event not found");
                }

                // ✅ ADD THIS - Pass organizer name to view
                ViewBag.OrganizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                return View("EventDetails", eventItem);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading event details: {ex.Message}");
                return View("Error");
            }
        }

        [HttpDelete("organizer/events/delete/{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                var eventItem = await _context.Events
                    .Include(e => e.Bookings)
                    .Include(e => e.Client)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(e => e.EventID == id && e.OrganizerID == organizer.OrganizerID);

                if (eventItem == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // ✅ UNLINK ORGANIZER FROM EVENT INSTEAD OF DELETING
                // This keeps the event for the client but removes it from organizer's view

                // Remove organizer association
                eventItem.OrganizerID = null;

                // Update status based on current state
                if (eventItem.Status == "Confirmed" || eventItem.Status == "Pending")
                {
                    eventItem.Status = "Planning"; // Reset to planning if it was active
                }
                // Keep Completed/Cancelled status as is for client's records

                eventItem.UpdatedAt = DateTime.Now;

                // ✅ UPDATE OR REMOVE BOOKINGS
                if (eventItem.Bookings != null && eventItem.Bookings.Any())
                {
                    foreach (var booking in eventItem.Bookings)
                    {
                        // Cancel bookings that were pending/confirmed
                        if (booking.Status == "Pending" || booking.Status == "Confirmed")
                        {
                            booking.Status = "Cancelled";
                            booking.ProviderNotes = $"Organizer disconnected from event on {DateTime.Now:yyyy-MM-dd}";
                        }
                    }

                    _logger.LogInformation($"Updated {eventItem.Bookings.Count} bookings for event {id}");
                }

                // ✅ NOTIFY CLIENT ABOUT ORGANIZER REMOVAL
                if (eventItem.Client?.UserID != null)
                {
                    var clientUserId = (int)eventItem.Client.UserID;
                    var organizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                    await _notificationService.CreateNotificationAsync(
                        userId: clientUserId,
                        title: "Organizer Removed from Event",
                        message: $"{organizerName} has removed themselves from your event '{eventItem.EventName}'. You may need to find a new organizer.",
                        notificationType: "EventUpdate",
                        redirectUrl: $"/client/events/details/{eventItem.EventID}"
                    );

                    _logger.LogInformation($"Notification sent to Client UserID: {clientUserId} about organizer removal");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Organizer {organizer.OrganizerID} unlinked from Event {id}. Event preserved for client.");

                return Json(new { success = true, message = "Event removed from your list. Client's event record has been preserved." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing event");
                return Json(new { success = false, message = "An error occurred while removing the event" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                // ✅ FIXED: Calculate completed BOOKINGS instead of completed EVENTS
                var completedBookings = await _context.Bookings
                    .Where(b => b.ProviderID == organizer.OrganizerID &&
                               b.ProviderType == "Organizer" &&
                               b.Status != null &&
                               b.Status.ToLower() == "completed")
                    .CountAsync();

                // Get unread messages count
                var unreadMessages = await GetUnreadMessagesCount(userId);

                // 🔍 DEBUG: Log the count
                Console.WriteLine($"Organizer {organizer.OrganizerID} has {completedBookings} completed bookings");

                var viewModel = new OrganizerProfileViewModel
                {
                    Organizer = organizer,
                    CompletedEvents = completedBookings, // This will now show completed bookings count
                    UnreadMessages = unreadMessages
                };

                return View("MyProfile", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer profile: {ex.Message}");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] Organizer updatedOrganizer)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var existingOrganizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (existingOrganizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                // Update only the allowed properties (prevent over-posting)
                existingOrganizer.BusinessName = updatedOrganizer.BusinessName?.Trim();
                existingOrganizer.BusinessDescription = updatedOrganizer.BusinessDescription?.Trim();
                existingOrganizer.ServiceAreas = updatedOrganizer.ServiceAreas?.Trim();
                existingOrganizer.YearsOfExperience = updatedOrganizer.YearsOfExperience;
                existingOrganizer.BusinessLicense = updatedOrganizer.BusinessLicense?.Trim();

                // Update user phone if provided
                if (existingOrganizer.User != null && !string.IsNullOrEmpty(updatedOrganizer.User?.Phone))
                {
                    existingOrganizer.User.Phone = updatedOrganizer.User.Phone.Trim();
                }

                // Validate required fields
                if (string.IsNullOrEmpty(existingOrganizer.BusinessName))
                {
                    return Json(new { success = false, message = "Business name is required" });
                }

                if (string.IsNullOrEmpty(existingOrganizer.BusinessDescription))
                {
                    return Json(new { success = false, message = "Business description is required" });
                }

                // Save changes to database
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating organizer profile: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating your profile" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                var bookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .Where(b => b.ProviderID == organizer.OrganizerID &&
                               b.ProviderType == "Organizer")
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new OrganizerBookingsViewModel
                {
                    Organizer = organizer,
                    Bookings = bookings,
                    UnreadMessages = unreadMessages
                };

                return View("BookingsRequests", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer bookings: {ex.Message}");
                return View("Error");
            }
        }

        [HttpPost]
        [Route("organizer/bookings/update-status/{id}")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
        {
            try
            {
                Console.WriteLine($"UpdateBookingStatus called - BookingID: {id}, Status: {request.Status}");

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    Console.WriteLine("ERROR: User not authenticated");
                    return Unauthorized();
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    Console.WriteLine("ERROR: Organizer not found");
                    return NotFound("Organizer not found");
                }

                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(b => b.BookingID == id &&
                                             b.ProviderID == organizer.OrganizerID &&
                                             b.ProviderType == "Organizer");

                if (booking == null)
                {
                    Console.WriteLine($"ERROR: Booking not found - ID: {id}");
                    return NotFound("Booking not found");
                }

                // Capitalize first letter of status
                booking.Status = char.ToUpper(request.Status[0]) + request.Status.Substring(1).ToLower();
                booking.ProviderNotes = $"Status updated to {booking.Status} on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                // Update event status
                if (booking.Event != null)
                {
                    if (booking.Status == "Confirmed")
                    {
                        booking.Event.Status = "Confirmed";
                        booking.Event.UpdatedAt = DateTime.Now;
                    }
                    else if (booking.Status == "Cancelled")
                    {
                        booking.Event.Status = "Cancelled";
                        booking.Event.UpdatedAt = DateTime.Now;
                    }
                    else if (booking.Status == "Rejected")
                    {
                        booking.Event.Status = "Planning";
                        booking.Event.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                // ========== CREATE NOTIFICATION FOR CLIENT ==========
                if (booking.Client?.UserID != null)
                {
                    var clientUserId = (int)booking.Client.UserID;
                    var organizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                    await _notificationService.CreateClientBookingStatusNotificationAsync(
                        clientUserId: clientUserId,
                        bookingId: booking.BookingID,
                        status: booking.Status,
                        organizerName: organizerName
                    );

                    Console.WriteLine($"Notification sent to Client UserID: {clientUserId}");
                }
                // ========== END NOTIFICATION ==========

                Console.WriteLine($"SUCCESS: Booking {id} status updated to {booking.Status}");

                return Ok(new { message = "Booking status updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR updating booking status: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to update booking status", details = ex.Message });
            }
        }

        [HttpGet]
        [Route("organizer/bookings/details/{id}")]
        public async Task<IActionResult> BookingDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .Include(o => o.User)  // ✅ Make sure this is here
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                var booking = await _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Event)
                    .FirstOrDefaultAsync(b => b.BookingID == id &&
                                             b.ProviderID == organizer.OrganizerID &&
                                             b.ProviderType == "Organizer");

                if (booking == null)
                {
                    TempData["ErrorMessage"] = $"Booking #{id} not found. It may have been deleted or you don't have access to it.";
                    return RedirectToAction("Bookings");
                }

                // ✅ This should already be here - if not, add it
                ViewBag.OrganizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                return View("BookingDetails", booking);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading booking details: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading booking details.";
                return RedirectToAction("Bookings");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null) return NotFound("Organizer not found");

                var services = await _context.Services
                    .Include(s => s.Gallery)
                    .Where(s => s.ProviderID == organizer.OrganizerID &&
                               s.ProviderType == "Organizer")
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new OrganizerServicesViewModel
                {
                    Organizer = organizer,
                    Services = services, // Empty list
                    UnreadMessages = unreadMessages
                };

                return View("MyServices", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer services: {ex.Message}");
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateService()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return RedirectToAction("Login", "Account");

            var organizer = await _context.Organizers
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.UserID == userId);

            if (organizer == null)
                return NotFound("Organizer not found");

            // Pass organizer to view
            ViewBag.Organizer = organizer;

            return View("CreateService");
        }

        // POST: Handle service creation
        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] Service service)
        {
            try
            {
                Console.WriteLine("=== CreateService Method Called ===");

                var userId = GetCurrentUserId();
                Console.WriteLine($"User ID: {userId}");

                if (userId == 0)
                {
                    Console.WriteLine("ERROR: User not authenticated");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    Console.WriteLine($"ERROR: Organizer not found for UserID: {userId}");
                    return Json(new { success = false, message = "Organizer not found" });
                }

                Console.WriteLine($"Organizer found: {organizer.BusinessName} (ID: {organizer.OrganizerID})");

                // Log received data
                Console.WriteLine($"ServiceName: {service.ServiceName}");
                Console.WriteLine($"ServiceDescription: {service.ServiceDescription}");
                Console.WriteLine($"Category: {service.Category}");
                Console.WriteLine($"BasePrice: {service.BasePrice}");
                Console.WriteLine($"Price: {service.Price}");
                Console.WriteLine($"PriceType: {service.PriceType}");
                Console.WriteLine($"Location: {service.Location}");
                Console.WriteLine($"IsActive: {service.IsActive}");

                // Validate required fields BEFORE setting any properties
                if (string.IsNullOrWhiteSpace(service.ServiceName))
                {
                    Console.WriteLine("ERROR: Service name is required");
                    return Json(new { success = false, message = "Service name is required" });
                }

                if (string.IsNullOrWhiteSpace(service.ServiceDescription))
                {
                    Console.WriteLine("ERROR: Service description is required");
                    return Json(new { success = false, message = "Service description is required" });
                }

                if (string.IsNullOrWhiteSpace(service.Category))
                {
                    Console.WriteLine("ERROR: Category is required");
                    return Json(new { success = false, message = "Category is required" });
                }

                if (service.BasePrice == null || service.BasePrice <= 0)
                {
                    Console.WriteLine("ERROR: Base price must be greater than 0");
                    return Json(new { success = false, message = "Base price must be greater than 0" });
                }

                if (service.Price <= 0)
                {
                    Console.WriteLine("ERROR: Price must be greater than 0");
                    return Json(new { success = false, message = "Price must be greater than 0" });
                }

                // Set service properties
                service.ProviderID = organizer.OrganizerID;
                service.ProviderType = "Organizer";
                service.CreatedAt = DateTime.Now;
                service.IsApproved = false;
                service.AverageRating = 0.00m;
                service.TotalReviews = 0;

                Console.WriteLine("Attempting to add service to database...");

                // Add service to database
                _context.Services.Add(service);

                Console.WriteLine("Calling SaveChangesAsync...");
                await _context.SaveChangesAsync();

                Console.WriteLine($"SUCCESS: Service created with ID: {service.ServiceID}");

                return Json(new { success = true, message = "Service created successfully", serviceId = service.ServiceID });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"DATABASE ERROR: {dbEx.Message}");
                Console.WriteLine($"Inner Exception: {dbEx.InnerException?.Message}");
                Console.WriteLine($"Stack Trace: {dbEx.StackTrace}");

                return Json(new
                {
                    success = false,
                    message = "Database error occurred. Check the console for details.",
                    error = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating service: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while creating your service",
                    error = ex.Message
                });
            }
        }

        // DELETE Service - Fixed route
        [HttpDelete("organizer/services/delete/{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == id &&
                                            s.ProviderID == organizer.OrganizerID &&
                                            s.ProviderType == "Organizer");

                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found" });
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Service deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting service: {ex.Message}");
                return Json(new { success = false, message = "Error deleting service" });
            }
        }

        [HttpPost("organizer/services/toggle/{id}")]
        public async Task<IActionResult> ToggleServiceStatus(int id, [FromBody] ToggleStatusRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == id &&
                                            s.ProviderID == organizer.OrganizerID &&
                                            s.ProviderType == "Organizer");

                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found" });
                }

                service.IsActive = request.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Service {(request.IsActive ? "activated" : "deactivated")} successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling service status: {ex.Message}");
                return Json(new { success = false, message = "Error updating service status" });
            }
        }

        // GET: Edit Service Page
        [HttpGet("organizer/services/edit/{id}")]
        public async Task<IActionResult> EditService(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0) return RedirectToAction("Login", "Account");

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null) return NotFound("Organizer not found");

                var service = await _context.Services
                    .Include(s => s.Gallery)
                    .FirstOrDefaultAsync(s => s.ServiceID == id &&
                                            s.ProviderID == organizer.OrganizerID &&
                                            s.ProviderType == "Organizer");

                if (service == null) return NotFound("Service not found");

                var viewModel = new EditServiceViewModel
                {
                    Organizer = organizer,
                    Service = service,
                    UnreadMessages = await GetUnreadMessagesCount(userId)
                };

                return View("EditService", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading edit service: {ex.Message}");
                return View("Error");
            }
        }

        // POST: Update Service
        [HttpPost("organizer/services/update/{id}")]
        public async Task<IActionResult> UpdateService(int id, [FromBody] Service updatedService)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "User not authenticated" });

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                    return Json(new { success = false, message = "Organizer not found" });

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == id &&
                                            s.ProviderID == organizer.OrganizerID &&
                                            s.ProviderType == "Organizer");

                if (service == null)
                    return Json(new { success = false, message = "Service not found" });

                // Validate required fields
                if (string.IsNullOrWhiteSpace(updatedService.ServiceName))
                    return Json(new { success = false, message = "Service name is required" });

                if (string.IsNullOrWhiteSpace(updatedService.Category))
                    return Json(new { success = false, message = "Category is required" });

                if (updatedService.Price <= 0)
                    return Json(new { success = false, message = "Price must be greater than 0" });

                // Update service properties
                service.ServiceName = updatedService.ServiceName.Trim();
                service.ServiceDescription = updatedService.ServiceDescription?.Trim();
                service.Category = updatedService.Category.Trim();
                service.BasePrice = updatedService.BasePrice;
                service.Price = updatedService.Price;
                service.PriceType = updatedService.PriceType?.Trim();
                service.Location = updatedService.Location?.Trim();
                service.IsActive = updatedService.IsActive;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Service updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating service: {ex.Message}");
                return Json(new { success = false, message = "Error updating service" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Earnings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                // Calculate earnings data
                var totalEarnings = await CalculateTotalEarnings(organizer.OrganizerID);
                var availableBalance = await CalculateAvailableBalance(organizer.OrganizerID);
                var monthlyEarnings = await CalculateMonthlyEarnings(organizer.OrganizerID);
                var pendingPayouts = await CalculatePendingPayouts(organizer.OrganizerID);

                // Get next payout information
                var (amount, date) = await GetNextPayoutInfo(organizer.OrganizerID);

                // Get transactions
                var transactions = await GetOrganizerTransactions(organizer.OrganizerID);

                // Get unread messages count
                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new OrganizerEarningsViewModel
                {
                    Organizer = organizer,
                    TotalEarnings = totalEarnings,
                    AvailableBalance = availableBalance,
                    MonthlyEarnings = monthlyEarnings,
                    PendingPayouts = pendingPayouts,
                    NextPayoutAmount = amount,
                    NextPayoutDate = date,
                    PayoutProgress = CalculatePayoutProgress(availableBalance),
                    Transactions = transactions,
                    UnreadMessages = unreadMessages
                };

                return View("EarningsPayouts", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer earnings: {ex.Message}");
                return View("Error");
            }
        }

        // Helper methods for earnings calculations
        private async Task<decimal> CalculateTotalEarnings(int organizerId)
        {
            var completedBookings = await _context.Bookings
                .Where(b => b.ProviderID == organizerId &&
                           b.ProviderType == "Organizer" &&
                           b.Status != null &&
                           b.Status.ToLower() == "completed")
                .ToListAsync();

            decimal total = 0;
            foreach (var booking in completedBookings)
            {
                decimal amount = booking.FinalAmount ?? booking.QuoteAmount ?? 0;
                total += amount;
            }

            _logger.LogInformation($"Total Earnings for Organizer {organizerId}: ₱{total:N2} from {completedBookings.Count} completed bookings");
            return total;
        }

        private async Task<decimal> CalculateAvailableBalance(int organizerId)
        {
            var totalEarnings = await CalculateTotalEarnings(organizerId);

            var organizer = await _context.Organizers
                .FirstOrDefaultAsync(o => o.OrganizerID == organizerId);

            if (organizer == null)
            {
                _logger.LogWarning($"Organizer {organizerId} not found");
                return totalEarnings;
            }

            var completedPayouts = await _context.Payouts
                .Where(p => p.PayeeID == organizer.UserID &&
                           p.Status != null &&
                           p.Status.ToLower() == "completed")
                .SumAsync(p => p.Amount);

            var available = totalEarnings - completedPayouts;

            _logger.LogInformation($"Available Balance for Organizer {organizerId}: ₱{available:N2} (Earnings: ₱{totalEarnings:N2} - Payouts: ₱{completedPayouts:N2})");
            return available;
        }

        private async Task<decimal> CalculateMonthlyEarnings(int organizerId)
        {
            var currentMonth = DateTime.Today.Month;
            var currentYear = DateTime.Today.Year;

            var monthlyBookings = await _context.Bookings
                .Where(b => b.ProviderID == organizerId &&
                           b.ProviderType == "Organizer" &&
                           b.Status != null &&
                           b.Status.ToLower() == "completed" &&
                           b.EventDate.Month == currentMonth &&
                           b.EventDate.Year == currentYear)
                .ToListAsync();

            decimal monthlyTotal = 0;
            foreach (var booking in monthlyBookings)
            {
                decimal amount = booking.FinalAmount ?? booking.QuoteAmount ?? 0;
                monthlyTotal += amount;
            }

            _logger.LogInformation($"Monthly Earnings for Organizer {organizerId}: ₱{monthlyTotal:N2} from {monthlyBookings.Count} bookings this month");
            return monthlyTotal;
        }

        private async Task<decimal> CalculatePendingPayouts(int organizerId)
        {
            var organizer = await _context.Organizers
                .FirstOrDefaultAsync(o => o.OrganizerID == organizerId);

            if (organizer == null)
            {
                _logger.LogWarning($"Organizer {organizerId} not found");
                return 0;
            }

            var pendingPayouts = await _context.Payouts
                .Where(p => p.PayeeID == organizer.UserID &&
                           p.Status != null &&
                           p.Status.ToLower() == "pending")
                .SumAsync(p => p.Amount);

            _logger.LogInformation($"Pending Payouts for Organizer {organizerId}: ₱{pendingPayouts:N2}");
            return pendingPayouts;
        }

        private async Task<(decimal amount, DateTime date)> GetNextPayoutInfo(int organizerId)
        {
            // Simple implementation - next payout is available balance, scheduled for end of month
            var availableBalance = await CalculateAvailableBalance(organizerId);
            var nextPayoutDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                .AddMonths(1)
                .AddDays(-1); // Last day of current month

            return (availableBalance, nextPayoutDate);
        }

        private static int CalculatePayoutProgress(decimal availableBalance)
        {
            const decimal minimumPayout = 100m;
            var progress = (int)((availableBalance / minimumPayout) * 100);
            return Math.Min(progress, 100);
        }

        private async Task<List<Transaction>> GetOrganizerTransactions(int organizerId)
        {
            return await _context.Transactions
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Client)
                        .ThenInclude(c => c.User)
                .Include(t => t.Payer) // Include payer info
                .Where(t => t.PayeeID == organizerId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(20)
                .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return NotFound("Organizer not found");
                }

                // Get all conversation IDs where the organizer is a participant
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                // ========== FIXED: Load Client and Organizer directly from Conversation ==========
                var conversations = await _context.Conversations
                    .Include(c => c.Client)              // Load Client directly
                        .ThenInclude(cl => cl.User)      // Then load User
                    .Include(c => c.Organizer)           // Load Organizer directly
                        .ThenInclude(o => o.User)        // Then load User
                    .Include(c => c.Event)               // Event is optional now
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Sender)
                    .Where(c => conversationIds.Contains(c.ConversationID))
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();
                // ========== END FIX ==========

                // Get messages for the first conversation (or all if you prefer)
                var messages = conversations.Any()
                    ? await _context.Messages
                        .Include(m => m.Sender)
                        .Where(m => m.ConversationID == conversations.First().ConversationID)
                        .OrderBy(m => m.SentAt)
                        .ToListAsync()
                    : new List<Message>();

                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new OrganizerMessagesViewModel
                {
                    Organizer = organizer,
                    UnreadMessages = unreadMessages,
                    Conversations = conversations,
                    Messages = messages
                };

                return View("Messages", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading organizer messages: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View("Error");
            }
        }

        [HttpGet("organizer/messages/conversation/{conversationId}")]
        public async Task<IActionResult> GetConversationMessages(int conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify user is part of this conversation
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationID == conversationId && cp.UserID == userId && cp.LeftAt == null);

                if (!isParticipant)
                {
                    return Json(new { success = false, message = "Not authorized" });
                }

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ConversationID == conversationId)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new
                    {
                        messageId = m.MessageID,
                        messageText = m.MessageText,
                        sentAt = m.SentAt,
                        senderRole = m.Sender.Role,
                        senderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        isRead = m.IsRead
                    })
                    .ToListAsync();

                // Mark messages as read
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationID == conversationId && !m.IsRead && m.SenderID != userId)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading conversation messages: {ex.Message}");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }

        [HttpPost("organizer/messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationID == request.ConversationId &&
                                   cp.UserID == userId &&
                                   cp.LeftAt == null);

                if (!isParticipant)
                {
                    return Json(new { success = false, message = "Not authorized" });
                }

                var message = new Message
                {
                    ConversationID = request.ConversationId,
                    SenderID = userId,
                    MessageText = request.MessageText,
                    MessageType = "Text",
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);

                // Update conversation last message time
                var conversation = await _context.Conversations
                    .Include(c => c.Client)
                        .ThenInclude(cl => cl.User)
                    .Include(c => c.Organizer)
                        .ThenInclude(o => o.User)
                    .FirstOrDefaultAsync(c => c.ConversationID == request.ConversationId);

                if (conversation != null)
                {
                    conversation.LastMessageAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // ========== ADD NOTIFICATION FOR NEW MESSAGE ==========
                if (conversation != null)
                {
                    var sender = await _context.Users.FindAsync(userId);
                    var senderName = $"{sender?.FirstName} {sender?.LastName}";

                    // Get the other participant (recipient)
                    var recipientUserId = await _context.ConversationParticipants
                        .Where(cp => cp.ConversationID == request.ConversationId &&
                                    cp.UserID != userId &&
                                    cp.LeftAt == null)
                        .Select(cp => cp.UserID)
                        .FirstOrDefaultAsync();

                    if (recipientUserId != 0)
                    {
                        await _notificationService.CreateMessageNotificationAsync(
                            userId: recipientUserId,
                            conversationId: request.ConversationId,
                            senderName: senderName
                        );

                        Console.WriteLine($"Message notification sent to UserID: {recipientUserId}");
                    }
                }
                // ========== END NOTIFICATION ==========

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpPost("organizer/messages/start-conversation")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { success = false, message = "Organizer not found" });
                }

                // Get the booking with related data
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(b => b.BookingID == request.BookingId &&
                                             b.ProviderID == organizer.OrganizerID &&
                                             b.ProviderType == "Organizer");

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found" });
                }

                if (booking.Client?.User == null)
                {
                    return Json(new { success = false, message = "Client information not found" });
                }

                // ========== FIXED: Check if conversation exists between organizer and client ==========
                var existingConversation = await _context.Conversations
                    .Where(c => c.OrganizerID == organizer.OrganizerID &&
                               c.ClientID == booking.ClientID)
                    .Select(c => c.ConversationID)
                    .FirstOrDefaultAsync();

                if (existingConversation != 0)
                {
                    Console.WriteLine($"Found existing conversation {existingConversation} between Organizer {organizer.OrganizerID} and Client {booking.ClientID}");
                    return Json(new { success = true, conversationId = existingConversation });
                }
                // ========== END FIX ==========

                Console.WriteLine($"Creating new conversation between Organizer {organizer.OrganizerID} and Client {booking.ClientID}");

                // Create new conversation
                var conversation = new Conversation
                {
                    EventID = booking.EventID,
                    ClientID = booking.ClientID,
                    OrganizerID = organizer.OrganizerID,
                    ConversationType = "Direct",
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                // Add participants (organizer and client)
                var participants = new List<ConversationParticipant>
        {
            new ConversationParticipant
            {
                ConversationID = conversation.ConversationID,
                UserID = userId,
                JoinedAt = DateTime.Now
            },
            new ConversationParticipant
            {
                ConversationID = conversation.ConversationID,
                UserID = (int)booking.Client.UserID,
                JoinedAt = DateTime.Now
            }
        };

                _context.ConversationParticipants.AddRange(participants);
                await _context.SaveChangesAsync();

                // Send initial system message
                var systemMessage = new Message
                {
                    ConversationID = conversation.ConversationID,
                    SenderID = userId,
                    MessageText = $"Started conversation about {booking.Event?.EventName ?? "your event"}",
                    MessageType = "System",
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(systemMessage);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Created new conversation {conversation.ConversationID}");
                return Json(new { success = true, conversationId = conversation.ConversationID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting conversation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error starting conversation" });
            }
        }

        [HttpDelete("organizer/messages/delete/{conversationId}")]
        public async Task<IActionResult> DeleteConversation(int conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify user is part of this conversation
                var participant = await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp => cp.ConversationID == conversationId &&
                                              cp.UserID == userId);

                if (participant == null)
                {
                    return Json(new { success = false, message = "Conversation not found or unauthorized" });
                }

                // Get the conversation with all related data
                var conversation = await _context.Conversations
                    .Include(c => c.Messages)
                    .Include(c => c.Participants)
                    .FirstOrDefaultAsync(c => c.ConversationID == conversationId);

                if (conversation == null)
                {
                    return Json(new { success = false, message = "Conversation not found" });
                }

                // Delete all messages first
                if (conversation.Messages != null && conversation.Messages.Any())
                {
                    _context.Messages.RemoveRange(conversation.Messages);
                }

                // Delete all participants
                if (conversation.Participants != null && conversation.Participants.Any())
                {
                    _context.ConversationParticipants.RemoveRange(conversation.Participants);
                }

                // Delete the conversation itself
                _context.Conversations.Remove(conversation);

                await _context.SaveChangesAsync();

                Console.WriteLine($"Conversation {conversationId} deleted successfully by User {userId}");

                return Json(new { success = true, message = "Conversation deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting conversation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Error deleting conversation" });
            }
        }
        // Get unread notifications count
        [HttpGet("organizer/notifications/unread-count")]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false });

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return Json(new { success = false, count = 0 });
            }
        }

        // GET: Get all notifications for organizer
        [HttpGet("organizer/notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var notifications = await _notificationService.GetUserNotificationsAsync(userId, 20);

                var notificationList = notifications.Select(n => new
                {
                    id = n.NotificationID,
                    title = n.Title,
                    message = n.Message,
                    type = n.NotificationType,
                    isRead = n.IsRead,
                    redirectUrl = n.RedirectUrl,
                    timeAgo = GetTimeAgo(n.CreatedAt)
                }).ToList();

                return Json(new { success = true, notifications = notificationList });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
                return Json(new { success = false, message = "Error loading notifications" });
            }
        }

        // GET: Get unread notification count
        [HttpGet("organizer/notifications/unread-count")]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, count = 0 });
                }

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return Json(new { success = false, count = 0 });
            }
        }

        // POST: Mark notification as read
        [HttpPost("organizer/notifications/mark-read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var success = await _notificationService.MarkAsReadAsync(id);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notification as read: {ex.Message}");
                return Json(new { success = false });
            }
        }

        // POST: Mark all notifications as read
        [HttpPost("organizer/notifications/mark-all-read")]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking all as read: {ex.Message}");
                return Json(new { success = false });
            }
        }

        // Helper method to calculate "time ago"
        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";

            return dateTime.ToString("MMM dd");
        }
        // In OrganizerController.cs, add this new method (after the other notification methods):

        [HttpDelete("organizer/notifications/delete/{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Verify the notification belongs to this user
                var notification = await _context.Notifications.FindAsync(id);

                if (notification == null)
                {
                    return Json(new { success = false, message = "Notification not found" });
                }

                if (notification.UserID != userId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Delete the notification
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Notification {id} deleted by User {userId}");

                return Json(new { success = true, message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting notification: {ex.Message}");
                return Json(new { success = false, message = "Error deleting notification" });
            }
        }
        [Authorize(Roles = "Organizer")]
        [HttpGet("organizer/reviews")]
        public async Task<IActionResult> MyReviews()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return RedirectToAction("Login", "Account");

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                    return NotFound("Organizer not found");

                // Get all reviews for this organizer
                var reviews = await _context.Reviews
                    .Where(r => r.RevieweeID == organizer.OrganizerID &&
                               r.RevieweeType == "Organizer" &&
                               r.IsApproved)
                    .Include(r => r.Reviewer)
                    .Include(r => r.Booking)
                    .ThenInclude(b => b.Service)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reviewViewModels = reviews.Select(r => new
                {
                    r.ReviewID,
                    ReviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}",
                    ServiceName = r.Booking?.Service?.ServiceName ?? "N/A",
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt,
                    IsEdited = r.IsEdited
                }).ToList();

                return View(new
                {
                    Organizer = organizer,
                    Reviews = reviewViewModels,
                    AverageRating = organizer.AverageRating,
                    TotalReviews = organizer.TotalReviews
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer reviews");
                return View("Error");
            }
        }
        [Authorize(Roles = "Organizer")]
        [HttpGet("organizer/api/reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"GetMyReviews API called by User {userId}");

                if (userId == 0)
                    return Json(new { success = false, message = "User not authenticated" });

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    _logger.LogWarning($"Organizer not found for User {userId}");
                    return Json(new { success = false, message = "Organizer not found" });
                }

                _logger.LogInformation($"Organizer found: OrganizerID={organizer.OrganizerID}");

                // Get all reviews for this organizer
                var reviews = await _context.Reviews
                    .Where(r => r.RevieweeID == organizer.OrganizerID &&
                               r.RevieweeType == "Organizer" &&
                               r.IsApproved)
                    .Include(r => r.Reviewer)
                    .Include(r => r.Booking)
                        .ThenInclude(b => b.Service)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation($"Found {reviews.Count} reviews for organizer {organizer.OrganizerID}");

                var reviewData = reviews.Select(r => new
                {
                    reviewId = r.ReviewID,
                    reviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}",
                    serviceName = r.Booking?.Service?.ServiceName ?? "Service",
                    rating = r.Rating,
                    reviewText = r.ReviewText,
                    createdAt = r.CreatedAt.ToString("MMM dd, yyyy"),
                    isEdited = r.IsEdited
                }).ToList();

                return Json(new
                {
                    success = true,
                    organizerName = organizer.BusinessName,
                    averageRating = organizer.AverageRating,
                    totalReviews = organizer.TotalReviews,
                    reviews = reviewData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return Json(new { success = false, message = "Error loading reviews" });
            }
        }

        [Authorize(Roles = "Organizer")]
        [HttpGet("organizer/api/service/{serviceId}/reviews")]
        public async Task<IActionResult> GetServiceReviews(int serviceId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "User not authenticated" });

                // Verify service belongs to this organizer
                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == serviceId && s.ProviderID == userId);

                if (service == null)
                    return Json(new { success = false, message = "Service not found" });

                // Get reviews for this service through bookings
                var reviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .Include(r => r.Reviewer)
                    .Where(r => r.Booking.ServiceID == serviceId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var reviewData = reviews.Select(r => new
                {
                    reviewId = r.ReviewID,
                    reviewerName = $"{r.Reviewer?.FirstName} {r.Reviewer?.LastName}",
                    rating = r.Rating,
                    reviewText = r.ReviewText,
                    createdAt = r.CreatedAt.ToString("MMM dd, yyyy")
                }).ToList();

                var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                return Json(new
                {
                    success = true,
                    serviceName = service.ServiceName,
                    averageRating = avgRating,
                    totalReviews = reviews.Count,
                    reviews = reviewData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service reviews");
                return Json(new { success = false, message = "Error loading reviews" });
            }
        }
        [Authorize(Roles = "Organizer")]
        [HttpGet("organizer/diagnostic/rating")]
        public async Task<IActionResult> RatingDiagnostic()
        {
            try
            {
                var userId = GetCurrentUserId();
                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                    return Json(new { error = "Organizer not found" });

                // Get all reviews for this organizer
                var reviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .Include(r => r.Reviewer)
                    .Where(r => r.Booking.ProviderID == organizer.OrganizerID &&
                               r.Booking.ProviderType == "Organizer")
                    .ToListAsync();

                var approvedReviews = reviews.Where(r => r.IsApproved).ToList();

                var diagnostic = new
                {
                    organizerId = organizer.OrganizerID,
                    businessName = organizer.BusinessName,
                    currentAverageRating = organizer.AverageRating,
                    currentTotalReviews = organizer.TotalReviews,

                    actualTotalReviews = reviews.Count,
                    approvedReviewsCount = approvedReviews.Count,

                    calculatedAverage = approvedReviews.Any()
                        ? approvedReviews.Average(r => r.Rating)
                        : 0,

                    reviews = reviews.Select(r => new
                    {
                        reviewId = r.ReviewID,
                        reviewerName = r.Reviewer != null
                            ? $"{r.Reviewer.FirstName} {r.Reviewer.LastName}"
                            : "Unknown",
                        rating = r.Rating,
                        isApproved = r.IsApproved,
                        reviewText = r.ReviewText,
                        createdAt = r.CreatedAt
                    }).ToList()
                };

                return Json(diagnostic);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("organizer/api/verify-booking/{id}")]
        public async Task<IActionResult> VerifyBookingExists(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { exists = false });
                }

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                {
                    return Json(new { exists = false });
                }

                var bookingExists = await _context.Bookings
                    .AnyAsync(b => b.BookingID == id &&
                                  b.ProviderID == organizer.OrganizerID &&
                                  b.ProviderType == "Organizer");

                return Json(new { exists = bookingExists });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying booking: {ex.Message}");
                return Json(new { exists = false });
            }
        }

        [HttpPost("organizer/earnings/request-payout")]
        public async Task<IActionResult> RequestPayout([FromBody] PayoutRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                    return Json(new { success = false, message = "User not authenticated" });

                var organizer = await _context.Organizers
                    .FirstOrDefaultAsync(o => o.UserID == userId);

                if (organizer == null)
                    return Json(new { success = false, message = "Organizer not found" });

                var availableBalance = await CalculateAvailableBalance(organizer.OrganizerID);

                if (request.Amount <= 0)
                    return Json(new { success = false, message = "Payout amount must be greater than 0" });

                if (request.Amount > availableBalance)
                    return Json(new
                    {
                        success = false,
                        message = $"Insufficient balance. Available: ₱{availableBalance:N2}, Requested: ₱{request.Amount:N2}"
                    });

                const decimal minimumPayout = 1000m; // Changed to ₱1000 minimum
                if (request.Amount < minimumPayout)
                    return Json(new
                    {
                        success = false,
                        message = $"Minimum payout amount is ₱{minimumPayout:N2}"
                    });

                var payout = new Payout
                {
                    PayeeID = userId,
                    Amount = request.Amount,
                    Status = "Pending",
                    PayoutMethod = request.PayoutMethod ?? "Bank Transfer",
                    CreatedAt = DateTime.Now
                };

                _context.Payouts.Add(payout);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payout request created: PayoutID={payout.PayoutID}, Amount=₱{request.Amount:N2}, User={userId}, Organizer={organizer.OrganizerID}");

                return Json(new
                {
                    success = true,
                    message = $"Payout request of ₱{request.Amount:N2} submitted successfully",
                    payoutId = payout.PayoutID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting payout");
                return Json(new { success = false, message = "Error processing payout request" });
            }
        }

    }
}