using Lumera.Data;
using Lumera.Models;
using Lumera.Models.ViewModels;
using Lumera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lumera.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController(ILogger<ClientController> logger, ApplicationDbContext context, INotificationService notificationService, IBookingService bookingService) : Controller
    {
        private readonly ILogger<ClientController> _logger = logger;
        private readonly ApplicationDbContext _context = context;
        private readonly INotificationService _notificationService = notificationService;
        private readonly IBookingService _bookingService = bookingService;  // ADD THIS

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        client = new Client
                        {
                            UserID = userId,
                            User = user,
                            NewsletterSubscription = true
                        };
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return NotFound("User not found");
                    }
                }

                // *** FIX: Calculate unread messages properly ***
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadMessagesCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) &&
                               !m.IsRead &&
                               m.SenderID != userId)
                    .CountAsync();

                var viewModel = new ClientDashboardViewModel
                {
                    Client = client,
                    UpcomingEvents = await GetUpcomingEvents(client.ClientID),
                    RecentBookings = await GetRecentBookings(client.ClientID),
                    RecentMessages = await GetRecentMessages(client.ClientID),
                    RecentReviews = await GetRecentReviews(client.ClientID),
                    UnreadMessages = unreadMessagesCount  // ADD THIS LINE
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client dashboard");
                return View("Error", new ErrorViewModel { RequestId = "Database connection error" });
            }
        }

        // GET USER EVENTS for booking
        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> GetUserEvents()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return Json(new { success = false, message = "Client not found" });
                }

                // ✅ FIXED: Exclude both Cancelled AND Completed events
                var events = await _context.Events
                    .Where(e => e.ClientID == client.ClientID &&
                               e.EventDate >= DateTime.Today &&
                               e.Status != "Cancelled" &&
                               e.Status != "Completed")  // ✅ ADD THIS LINE
                    .OrderBy(e => e.EventDate)
                    .Select(e => new
                    {
                        eventID = e.EventID,
                        eventName = e.EventName,
                        eventDate = e.EventDate,
                        eventType = e.EventType
                    })
                    .ToListAsync();

                return Json(new { success = true, events });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user events");
                return Json(new { success = false, message = "Error loading events" });
            }
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Events(string filter = "all")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("LoginSignup", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                // Get all events for this client based on status filter
                IQueryable<Event> eventsQuery = _context.Events
                    .Where(e => e.ClientID == client.ClientID);

                // Apply status filters (same as organizer)
                switch (filter.ToLower())
                {
                    case "pending":
                        eventsQuery = eventsQuery
                            .Where(e => e.Status == "Pending")
                            .OrderBy(e => e.EventDate);
                        break;
                    case "confirmed":
                        eventsQuery = eventsQuery
                            .Where(e => e.Status == "Confirmed")
                            .OrderBy(e => e.EventDate);
                        break;
                    case "completed":
                        eventsQuery = eventsQuery
                            .Where(e => e.Status == "Completed")
                            .OrderByDescending(e => e.EventDate);
                        break;
                    case "cancelled":
                        eventsQuery = eventsQuery
                            .Where(e => e.Status == "Cancelled")
                            .OrderByDescending(e => e.EventDate);
                        break;
                    case "all":
                    default:
                        eventsQuery = eventsQuery
                            .OrderByDescending(e => e.EventDate);
                        break;
                }

                var events = await eventsQuery.ToListAsync();

                // Get unread messages count
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadMessages = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) && !m.IsRead && m.SenderID != userId)
                    .CountAsync();

                // Create the view model
                var viewModel = new ClientEventsViewModel
                {
                    Client = client,
                    Events = events,
                    UnreadMessages = unreadMessages
                };

                // Pass the current filter to the view
                ViewBag.CurrentFilter = filter.ToLower();

                // Calculate counts for each status
                var allEvents = await _context.Events
                    .Where(e => e.ClientID == client.ClientID)
                    .ToListAsync();

                ViewBag.AllCount = allEvents.Count;
                ViewBag.PendingCount = allEvents.Count(e => e.Status == "Pending");
                ViewBag.ConfirmedCount = allEvents.Count(e => e.Status == "Confirmed");
                ViewBag.CompletedCount = allEvents.Count(e => e.Status == "Completed");
                ViewBag.CancelledCount = allEvents.Count(e => e.Status == "Cancelled");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events");
                return View("Error", new ErrorViewModel { RequestId = "Error loading events" });
            }
        }

        // CREATE EVENT - GET
        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                // Pass client name to ViewBag for display in sidebar
                ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";

                // Return view with empty Event model
                return View(new Event());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create event page");
                return View("Error", new ErrorViewModel { RequestId = "Error loading create event form" });
            }
        }

        // CREATE EVENT - POST
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                // Remove validation errors for properties we'll set manually
                ModelState.Remove("Client");
                ModelState.Remove("Organizer");
                ModelState.Remove("Bookings");

                if (!ModelState.IsValid)
                {
                    ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";
                    return View(model);
                }

                // Set the client ID and timestamps
                model.ClientID = client.ClientID;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.Status = "Draft"; // Default status

                // Add event to database
                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                // Log success
                _logger.LogInformation("Event created successfully: {EventName} by Client {ClientID}",
                    model.EventName, client.ClientID);

                // Redirect to events list with success message
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction("Events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == GetCurrentUserId());

                if (client != null)
                {
                    ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";
                }

                ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
                return View(model);
            }
        }
        // VIEW EVENT DETAILS - GET
        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == id && e.ClientID == client.ClientID);

                if (eventItem == null)
                {
                    return NotFound("Event not found");
                }

                // Get bookings for this event - load only the fields we need
                var bookingsFromDb = await _context.Bookings
                    .Where(b => b.EventID == id)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.ServiceID,
                        b.ProviderType,
                        b.Status,
                        b.FinalAmount,
                        b.QuoteAmount,
                        b.EventDate,
                        b.ProviderID
                    })
                    .ToListAsync();

                // Get service information separately
                var serviceIds = bookingsFromDb.Select(b => b.ServiceID).Distinct().ToList();
                var services = await _context.Services
                    .Where(s => serviceIds.Contains(s.ServiceID))
                    .Select(s => new { s.ServiceID, s.ServiceName })
                    .ToListAsync();

                // Map to Booking entities
                var bookings = new List<Booking>();
                foreach (var b in bookingsFromDb)
                {
                    var service = services.FirstOrDefault(s => s.ServiceID == b.ServiceID);
                    bookings.Add(new Booking
                    {
                        BookingID = b.BookingID,
                        ServiceID = b.ServiceID,
                        ProviderType = b.ProviderType,
                        Status = b.Status,
                        FinalAmount = b.FinalAmount,
                        QuoteAmount = b.QuoteAmount,
                        EventDate = b.EventDate,
                        ProviderID = b.ProviderID,
                        Service = service != null ? new Service
                        {
                            ServiceID = service.ServiceID,
                            ServiceName = service.ServiceName
                        } : null
                    });
                }

                ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";
                ViewBag.Bookings = bookings;

                return View(eventItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event details");
                return View("Error", new ErrorViewModel { RequestId = "Error loading event details" });
            }
        }

        // EDIT EVENT - GET
        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == id && e.ClientID == client.ClientID);

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

                ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";

                return View(eventItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit event page");
                return View("Error", new ErrorViewModel { RequestId = "Error loading event" });
            }
        }

        // EDIT EVENT - POST
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event model)
        {
            try
            {
                if (id != model.EventID)
                {
                    return NotFound();
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                var existingEvent = await _context.Events
                    .Include(e => e.Organizer)
                        .ThenInclude(o => o.User)
                    .FirstOrDefaultAsync(e => e.EventID == id && e.ClientID == client.ClientID);

                if (existingEvent == null)
                {
                    return NotFound("Event not found");
                }

                // ✅ PREVENT EDITING OF COMPLETED OR CANCELLED EVENTS
                if (existingEvent.Status == "Completed" || existingEvent.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = "Cannot edit completed or cancelled events.";
                    return RedirectToAction("EventDetails", new { id = existingEvent.EventID });
                }

                // Remove validation for navigation properties
                ModelState.Remove("Client");
                ModelState.Remove("Organizer");
                ModelState.Remove("Bookings");

                if (!ModelState.IsValid)
                {
                    ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";
                    return View(model);
                }

                // ✅ CHECK IF EVENT WAS CONFIRMED AND IS NOW BEING EDITED
                bool wasConfirmed = existingEvent.Status == "Confirmed";

                // Update event properties
                existingEvent.EventName = model.EventName;
                existingEvent.EventType = model.EventType;
                existingEvent.EventDescription = model.EventDescription;
                existingEvent.EventDate = model.EventDate;
                existingEvent.GuestCount = model.GuestCount;
                existingEvent.Budget = model.Budget;
                existingEvent.Location = model.Location;

                // ✅ IF EVENT WAS CONFIRMED, CHANGE STATUS TO PENDING
                if (wasConfirmed)
                {
                    existingEvent.Status = "Pending";
                    _logger.LogInformation($"Event {existingEvent.EventID} status changed from Confirmed to Pending after client edit");
                }
                else if (existingEvent.Status != "Completed" && existingEvent.Status != "Cancelled")
                {
                    // For Draft or Planning events, keep as is or update if provided
                    if (!string.IsNullOrEmpty(model.Status))
                    {
                        existingEvent.Status = model.Status;
                    }
                }

                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // ✅ NOTIFY ORGANIZER IF EVENT WAS CONFIRMED AND NOW PENDING
                if (wasConfirmed && existingEvent.OrganizerID.HasValue)
                {
                    var organizer = await _context.Organizers
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrganizerID == existingEvent.OrganizerID);

                    if (organizer?.UserID != null)
                    {
                        var clientName = $"{client.User?.FirstName} {client.User?.LastName}";

                        await _notificationService.CreateNotificationAsync(
                            userId: (int)organizer.UserID,
                            title: "Event Updated - Review Required",
                            message: $"{clientName} has updated the event '{existingEvent.EventName}'. Please review the changes.",
                            notificationType: "EventUpdate",
                            redirectUrl: $"/organizer/events/details/{existingEvent.EventID}"
                        );

                        _logger.LogInformation($"Notification sent to Organizer UserID: {organizer.UserID} for event {existingEvent.EventID}");
                    }
                }

                _logger.LogInformation("Event updated successfully: {EventName} by Client {ClientID}",
                    existingEvent.EventName, client.ClientID);

                TempData["SuccessMessage"] = wasConfirmed
                    ? "Event updated successfully! Status changed to Pending for organizer review."
                    : "Event updated successfully!";

                return RedirectToAction("Events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == GetCurrentUserId());

                if (client != null)
                {
                    ViewBag.ClientName = $"{client.User?.FirstName} {client.User?.LastName}";
                }

                ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
                return View(model);
            }
        }

        // DELETE EVENT - POST
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return Json(new { success = false, message = "Client not found" });
                }

                var eventItem = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == id && e.ClientID == client.ClientID);

                if (eventItem == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // Check if there are any bookings associated with this event
                var hasBookings = await _context.Bookings
                    .AnyAsync(b => b.EventID == id);

                if (hasBookings)
                {
                    return Json(new { success = false, message = "Cannot delete event with existing bookings" });
                }

                _context.Events.Remove(eventItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event deleted successfully: {EventName} by Client {ClientID}",
                    eventItem.EventName, client.ClientID);

                return Json(new { success = true, message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                return Json(new { success = false, message = "Error deleting event" });
            }
        }
        private async Task<List<Event>> GetUpcomingEvents(int clientId)
        {
            try
            {
                return await _context.Events
                    .Where(e => e.ClientID == clientId &&
                               e.EventDate >= DateTime.Today &&
                               e.Status != "Cancelled")  // ✅ ADD THIS LINE
                    .OrderBy(e => e.EventDate)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading upcoming events");
                return new List<Event>();
            }
        }

        private async Task<List<BookingViewModel>> GetRecentBookings(int clientId)
        {
            try
            {
                var bookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Where(b => b.ClientID == clientId &&
                               b.Status != "Cancelled")  // ✅ ADD THIS LINE
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .ToListAsync();

                // Convert Booking entities to BookingViewModel using correct property names
                return bookings.Select(b => new BookingViewModel
                {
                    BookingID = b.BookingID,
                    Service = b.Service,
                    ServiceName = b.Service?.ServiceName ?? string.Empty,
                    ProviderName = string.Empty, // You'll need to populate this
                    ProviderType = b.Service?.ProviderType ?? string.Empty,
                    Status = b.Status,
                    Price = b.FinalAmount ?? b.QuoteAmount ?? 0,
                    EventDate = b.EventDate
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading recent bookings");
                return new List<BookingViewModel>();
            }
        }

        private async Task<List<MessageViewModel>> GetRecentMessages(int clientId)
        {
            try
            {
                var user = await _context.Clients
                    .Where(c => c.ClientID == clientId)
                    .Select(c => c.User)
                    .FirstOrDefaultAsync();

                if (user == null) return new List<MessageViewModel>();

                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == user.UserID)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => conversationIds.Contains(m.ConversationID))
                    .OrderByDescending(m => m.SentAt)
                    .Take(5)
                    .ToListAsync();

                // Convert Message entities to MessageViewModel using correct property names
                return messages.Select(m => new MessageViewModel
                {
                    MessageID = m.MessageID,
                    SenderType = m.Sender?.Role ?? string.Empty,
                    Content = m.MessageText, // Use MessageText instead of Content
                    SentAt = m.SentAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading recent messages");
                return new List<MessageViewModel>();
            }
        }

        private async Task<List<ReviewViewModel>> GetRecentReviews(int clientId)
        {
            try
            {
                var user = await _context.Clients
                    .Where(c => c.ClientID == clientId)
                    .Select(c => c.User)
                    .FirstOrDefaultAsync();

                if (user == null) return new List<ReviewViewModel>();

                var reviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .ThenInclude(b => b.Service)
                    .Where(r => r.ReviewerID == user.UserID)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Convert Review entities to ReviewViewModel
                return reviews.Select(r => new ReviewViewModel
                {
                    ReviewID = r.ReviewID,
                    Booking = r.Booking,
                    Rating = r.Rating,
                    Comment = r.ReviewText ?? string.Empty,
                    CreatedAt = r.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading recent reviews");
                return new List<ReviewViewModel>();
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Bookings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                // If no client exists, create a mock one for viewing the interface
                if (client == null)
                {
                    client = new Client
                    {
                        ClientID = 1,
                        UserID = userId,
                        User = new User
                        {
                            UserID = userId,
                            FirstName = "John",
                            LastName = "Doe",
                            Email = "john.doe@example.com"
                        }
                    };

                    // Return early with empty data
                    var emptyViewModel = new ClientBookingsViewModel
                    {
                        Client = client,
                        Bookings = new List<BookingViewModel>(),
                        UnreadMessages = 0
                    };
                    return View(emptyViewModel);
                }

                // Get all bookings with ProviderID and ProviderType
                var bookingsFromDb = await _context.Bookings
                    .Where(b => b.ClientID == client.ClientID)
                    .OrderByDescending(b => b.BookingDate)
                    .Select(b => new
                    {
                        b.BookingID,
                        b.ServiceID,
                        b.ProviderID,
                        b.ProviderType,
                        b.Status,
                        b.FinalAmount,
                        b.QuoteAmount,
                        b.EventDate
                    })
                    .ToListAsync();

                // Get service information
                var serviceIds = bookingsFromDb.Select(b => b.ServiceID).Distinct().ToList();
                var services = await _context.Services
                    .Where(s => serviceIds.Contains(s.ServiceID))
                    .Select(s => new { s.ServiceID, s.ServiceName })
                    .ToListAsync();

                // ========== FIX: Get Organizer and Supplier names ==========
                // Get unique organizer IDs
                var organizerIds = bookingsFromDb
                    .Where(b => b.ProviderType == "Organizer")
                    .Select(b => b.ProviderID)
                    .Distinct()
                    .ToList();

                // Get unique supplier IDs
                var supplierIds = bookingsFromDb
                    .Where(b => b.ProviderType == "Supplier")
                    .Select(b => b.ProviderID)
                    .Distinct()
                    .ToList();

                // Fetch organizer names
                var organizers = await _context.Organizers
                    .Where(o => organizerIds.Contains(o.OrganizerID))
                    .Select(o => new { o.OrganizerID, o.BusinessName })
                    .ToListAsync();

                // Fetch supplier names
                var suppliers = await _context.Suppliers
                    .Where(s => supplierIds.Contains(s.SupplierID))
                    .Select(s => new { s.SupplierID, s.BusinessName })
                    .ToListAsync();
                // ========== END FIX ==========

                // Map bookings to view models with correct provider names
                var bookings = bookingsFromDb.Select(b =>
                {
                    var service = services.FirstOrDefault(s => s.ServiceID == b.ServiceID);

                    // ========== FIX: Get provider name based on ProviderType ==========
                    string providerName = "N/A";
                    if (b.ProviderType == "Organizer")
                    {
                        var organizer = organizers.FirstOrDefault(o => o.OrganizerID == b.ProviderID);
                        providerName = organizer?.BusinessName ?? "Unknown Organizer";
                    }
                    else if (b.ProviderType == "Supplier")
                    {
                        var supplier = suppliers.FirstOrDefault(s => s.SupplierID == b.ProviderID);
                        providerName = supplier?.BusinessName ?? "Unknown Supplier";
                    }
                    // ========== END FIX ==========

                    return new BookingViewModel
                    {
                        BookingID = b.BookingID,
                        Service = null,
                        ServiceName = service?.ServiceName ?? "N/A",
                        ProviderName = providerName,  // Now shows actual organizer/supplier name
                        ProviderType = b.ProviderType ?? "N/A",
                        Status = b.Status ?? "Unknown",
                        Price = b.FinalAmount ?? b.QuoteAmount ?? 0,
                        EventDate = b.EventDate
                    };
                }).ToList();

                // Get unread messages count
                var unreadCount = 0;
                try
                {
                    var conversationIds = await _context.ConversationParticipants
                        .Where(cp => cp.UserID == userId)
                        .Select(cp => cp.ConversationID)
                        .ToListAsync();

                    unreadCount = await _context.Messages
                        .Where(m => conversationIds.Contains(m.ConversationID) && !m.IsRead && m.SenderID != userId)
                        .CountAsync();
                }
                catch
                {
                    // If messages fail, just continue with 0
                }

                var viewModel = new ClientBookingsViewModel
                {
                    Client = client,
                    Bookings = bookings,
                    UnreadMessages = unreadCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bookings: {Message}", ex.Message);
                return View("Error", new ErrorViewModel { RequestId = $"Error loading bookings: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Browse(List<Models.ServiceViewModel> serviceCategoryViewModel, string search = "", string category = "", string location = "")
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                // If no client exists, create a mock one for viewing the interface
                if (client == null)
                {
                    client = new Client
                    {
                        ClientID = 1,
                        UserID = userId,
                        User = new User
                        {
                            UserID = userId,
                            FirstName = "John",
                            LastName = "Doe",
                            Email = "john.doe@example.com"
                        }
                    };

                    // Return early with empty data
                    var emptyViewModel = new ClientBrowseViewModel
                    {
                        Client = client,
                        Services = new List<Lumera.Models.ServiceViewModel>(),
                        UnreadMessages = 0
                    };
                    return View(emptyViewModel);
                }

                // Get all services with filters - Build query first
                var servicesQuery = _context.Services
                    .Where(s => s.IsActive && s.IsApproved)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    servicesQuery = servicesQuery.Where(s =>
                        s.ServiceName.Contains(search) ||
                        (s.ServiceDescription != null && s.ServiceDescription.Contains(search)));
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                {
                    servicesQuery = servicesQuery.Where(s => s.Category == category);
                }

                // Apply location filter
                if (!string.IsNullOrEmpty(location))
                {
                    servicesQuery = servicesQuery.Where(s =>
                        s.Location != null && s.Location.Contains(location));
                }

                // Load services from database first (without BookingCount)
                var servicesFromDb = await servicesQuery
                    .Select(s => new
                    {
                        s.ServiceID,
                        s.ServiceName,
                        s.ServiceDescription,
                        s.BasePrice,
                        s.Category
                    })
                    .ToListAsync();

                // Calculate ratings and review counts for each service
                var ServiceCategoryViewModel = new List<Lumera.Models.ServiceViewModel>();
                foreach (var service in servicesFromDb)
                {
                    // Get reviews for this service through bookings (only approved reviews)
                    var reviews = await _context.Reviews
                        .Include(r => r.Booking)
                        .Where(r => r.Booking.ServiceID == service.ServiceID && r.IsApproved)
                        .ToListAsync();

                    ServiceCategoryViewModel.Add(new Lumera.Models.ServiceViewModel
                    {
                        ServiceID = service.ServiceID,
                        ServiceName = service.ServiceName,
                        Description = service.ServiceDescription ?? string.Empty,
                        Price = service.BasePrice ?? 0,
                        Rating = reviews.Any() ? (int)Math.Round(reviews.Average(r => r.Rating)) : 0,
                        ReviewCount = reviews.Count,
                        Category = service.Category ?? string.Empty
                    });
                }

                // Get unread messages count
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) && !m.IsRead && m.SenderID != userId)
                    .CountAsync();

                var viewModel = new ClientBrowseViewModel
                {
                    Client = client,
                    Services = ServiceCategoryViewModel,
                    UnreadMessages = unreadCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading browse page");
                return View("Error", new ErrorViewModel { RequestId = "Error loading services" });
            }
        }

        [Authorize(Roles = "Client")]
        [HttpGet("client/service/{id}")]
        public async Task<IActionResult> ServiceDetails(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                // Get service details
                var service = await _context.Services
                    .Include(s => s.Gallery)
                    .FirstOrDefaultAsync(s => s.ServiceID == id && s.IsActive && s.IsApproved);

                if (service == null)
                {
                    return NotFound("Service not found or not available");
                }

                // Get provider information
                string providerName = "Provider";
                if (service.ProviderType == "Organizer")
                {
                    var organizer = await _context.Organizers
                        .FirstOrDefaultAsync(o => o.OrganizerID == service.ProviderID);
                    providerName = organizer?.BusinessName ?? "Unknown Organizer";
                }
                else if (service.ProviderType == "Supplier")
                {
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.SupplierID == service.ProviderID);
                    providerName = supplier?.BusinessName ?? "Unknown Supplier";
                }

                // Get reviews for this service
                var reviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .Include(r => r.Reviewer)
                    .Where(r => r.Booking.ServiceID == id && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Get unread messages count
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) && !m.IsRead && m.SenderID != userId)
                    .CountAsync();

                var viewModel = new ServiceDetailViewModel
                {
                    Client = client,
                    Service = service,
                    ProviderName = providerName,
                    Reviews = reviews,
                    UnreadMessages = unreadCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service details");
                return View("Error", new ErrorViewModel { RequestId = "Error loading service details" });
            }
        }

        // MESSAGES PAGE
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Messages(int? conversation = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                // Get all conversation IDs for this user
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                // Get conversations with all related data
                var conversations = await _context.Conversations
                    .Include(c => c.Event)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Sender)
                    .Where(c => conversationIds.Contains(c.ConversationID))
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                // Get provider names for each conversation
                var conversationViewModels = new List<ConversationViewModel>();
                foreach (var conv in conversations)
                {
                    var otherParticipant = await _context.ConversationParticipants
                        .Include(cp => cp.User)
                        .Where(cp => cp.ConversationID == conv.ConversationID && cp.UserID != userId)
                        .FirstOrDefaultAsync();

                    var lastMessage = conv.Messages?.OrderByDescending(m => m.SentAt).FirstOrDefault();
                    var unreadCount = conv.Messages?.Count(m => !m.IsRead && m.SenderID != userId) ?? 0;

                    conversationViewModels.Add(new ConversationViewModel
                    {
                        ConversationID = conv.ConversationID,
                        ServiceName = conv.Event?.EventName ?? "General",
                        ProviderName = otherParticipant?.User != null
                            ? $"{otherParticipant.User.FirstName} {otherParticipant.User.LastName}"
                            : "Provider",
                        LastMessage = lastMessage?.MessageText ?? "",
                        LastMessageTime = lastMessage?.SentAt ?? conv.CreatedAt,
                        UnreadCount = unreadCount
                    });
                }

                // Determine which conversation to show
                int selectedConversationId = 0;
                if (conversation.HasValue && conversationIds.Contains(conversation.Value))
                {
                    selectedConversationId = conversation.Value;
                }
                else if (conversations.Any())
                {
                    selectedConversationId = conversations.First().ConversationID;
                }

                // Get messages for the selected conversation
                var messages = selectedConversationId > 0
                    ? await _context.Messages
                        .Include(m => m.Sender)
                        .Where(m => m.ConversationID == selectedConversationId)
                        .OrderBy(m => m.SentAt)
                        .ToListAsync()
                    : new List<Message>();

                var unreadMessagesCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) && !m.IsRead && m.SenderID != userId)
                    .CountAsync();

                var viewModel = new ClientMessagesViewModel
                {
                    Client = client,
                    Conversations = conversationViewModels,
                    RecentMessages = messages.Select(m => new MessageViewModel
                    {
                        MessageID = m.MessageID,
                        // FIX: Set SenderType to "Client" if sender is current user, otherwise to sender's role
                        SenderType = m.SenderID == userId ? "Client" : (m.Sender?.Role ?? "Organizer"),
                        Content = m.MessageText,
                        SentAt = m.SentAt
                    }).ToList(),
                    UnreadMessages = unreadMessagesCount
                };

                ViewBag.SelectedConversationId = selectedConversationId;
                ViewBag.Messages = messages;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading messages");
                return View("Error", new ErrorViewModel { RequestId = "Error loading messages" });
            }
        }

        [HttpGet("client/messages/conversation/{conversationId}")]
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
                        content = m.MessageText,
                        sentAt = m.SentAt,
                        // FIX: Return "Client" for current user, otherwise sender's role
                        senderType = m.SenderID == userId ? "Client" : (m.Sender.Role ?? "Organizer"),
                        senderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        isRead = m.IsRead
                    })
                    .ToListAsync();

                // ? FIX: Mark ONLY messages sent TO YOU as read
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationID == conversationId &&
                               !m.IsRead &&
                               m.SenderID != userId)  // Only messages sent by OTHERS
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
                _logger.LogError(ex, "Error loading conversation messages");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }

        // API endpoint to send a message
        [HttpPost("client/messages/send")]
        public async Task<IActionResult> SendClientMessage([FromBody] SendMessageRequest request)
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
                    .AnyAsync(cp => cp.ConversationID == request.ConversationId && cp.UserID == userId && cp.LeftAt == null);

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

                // Update conversation last message time and get organizer info
                var conversation = await _context.Conversations
                    .Include(c => c.Organizer)
                        .ThenInclude(o => o.User)
                    .Include(c => c.Client)
                        .ThenInclude(cl => cl.User)
                    .FirstOrDefaultAsync(c => c.ConversationID == request.ConversationId);

                if (conversation != null)
                {
                    conversation.LastMessageAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // ========== CREATE NOTIFICATION FOR ORGANIZER ==========
                if (conversation != null)
                {
                    var sender = await _context.Users.FindAsync(userId);
                    var senderName = $"{sender?.FirstName} {sender?.LastName}";

                    // Get the organizer (recipient)
                    var organizerUserId = conversation.Organizer?.UserID;

                    if (organizerUserId != null && organizerUserId != 0)
                    {
                        await _notificationService.CreateMessageNotificationAsync(
                            userId: (int)organizerUserId,
                            conversationId: request.ConversationId,
                            senderName: senderName
                        );

                        _logger.LogInformation($"Message notification sent to Organizer UserID: {organizerUserId}");
                    }
                }
                // ========== END NOTIFICATION CODE ==========

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        // API endpoint to start a conversation from a booking
        [HttpPost("client/messages/start-conversation")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return Json(new { success = false, message = "Client not found" });
                }

                // Get the booking with related data
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .FirstOrDefaultAsync(b => b.BookingID == request.BookingId &&
                                             b.ClientID == client.ClientID);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found" });
                }

                // Get provider user ID and organizer ID
                int providerUserId = 0;
                int? organizerId = null;

                if (booking.ProviderType == "Organizer")
                {
                    var organizer = await _context.Organizers
                        .FirstOrDefaultAsync(o => o.OrganizerID == booking.ProviderID);
                    providerUserId = organizer?.UserID ?? 0;
                    organizerId = organizer?.OrganizerID;
                }
                else if (booking.ProviderType == "Supplier")
                {
                    var supplier = await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.SupplierID == booking.ProviderID);
                    providerUserId = supplier?.UserID ?? 0;
                }

                if (providerUserId == 0)
                {
                    return Json(new { success = false, message = "Provider not found" });
                }

                // Check if conversation already exists for this event between these users
                var existingConversation = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId)
                    .Select(cp => cp.Conversation)
                    .Where(c => c.EventID == booking.EventID)
                    .Select(c => c.ConversationID)
                    .FirstOrDefaultAsync();

                if (existingConversation != 0)
                {
                    return Json(new { success = true, conversationId = existingConversation });
                }

                // ========== FIXED: Set ClientID and OrganizerID ==========
                var conversation = new Conversation
                {
                    EventID = booking.EventID,
                    ClientID = client.ClientID,      // ADD THIS
                    OrganizerID = organizerId,       // ADD THIS (will be null for suppliers)
                    ConversationType = "Direct",      // ADD THIS
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };
                // ========== END FIX ==========

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                // Add participants (client and provider)
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
                UserID = providerUserId,
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

                return Json(new { success = true, conversationId = conversation.ConversationID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return Json(new { success = false, message = "Error starting conversation" });
            }
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Reviews()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"🔍 Loading reviews for user {userId}");

                if (userId == 0)
                    return RedirectToAction("Login", "Account");

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    _logger.LogWarning($"⚠️ Client not found for user {userId}");
                    var emptyViewModel = new ClientReviewsViewModel
                    {
                        Client = new Client
                        {
                            ClientID = 1,
                            UserID = userId,
                            User = new User
                            {
                                UserID = userId,
                                FirstName = "User",
                                LastName = ""
                            }
                        },
                        Reviews = new List<ReviewDetailViewModel>(),
                        PendingBookings = new List<PendingBookingViewModel>(),
                        UnreadMessages = 0
                    };
                    return View(emptyViewModel);
                }

                _logger.LogInformation($"✅ Client found: ClientID={client.ClientID}");

                // ✅ STEP 1: Get ALL reviews by this user
                var allReviews = await _context.Reviews
                    .Where(r => r.ReviewerID == userId)
                    .ToListAsync();

                _logger.LogInformation($"📊 Found {allReviews.Count} total reviews for user {userId}");

                // ✅ STEP 2: For each review, load the booking and service separately
                var reviewViewModels = new List<ReviewDetailViewModel>();

                foreach (var review in allReviews)
                {
                    if (!review.BookingID.HasValue)
                    {
                        _logger.LogWarning($"⚠️ Review {review.ReviewID} has no BookingID");
                        continue;
                    }

                    // Load booking
                    var booking = await _context.Bookings
                        .FirstOrDefaultAsync(b => b.BookingID == review.BookingID.Value);

                    if (booking == null)
                    {
                        _logger.LogWarning($"⚠️ Booking {review.BookingID.Value} not found for review {review.ReviewID}");
                        continue;
                    }

                    // Load service
                    var service = await _context.Services
                        .FirstOrDefaultAsync(s => s.ServiceID == booking.ServiceID);

                    if (service == null)
                    {
                        _logger.LogWarning($"⚠️ Service not found for booking {booking.BookingID}");
                        continue;
                    }

                    // ✅ Add to view models
                    reviewViewModels.Add(new ReviewDetailViewModel
                    {
                        ReviewID = review.ReviewID,
                        ServiceName = service.ServiceName,
                        Rating = review.Rating,
                        Comment = review.ReviewText ?? string.Empty,
                        CreatedAt = review.CreatedAt
                    });

                    _logger.LogInformation($"✅ Added review: ID={review.ReviewID}, Service={service.ServiceName}");
                }

                _logger.LogInformation($"📋 Total reviews to display: {reviewViewModels.Count}");

                // ✅ STEP 3: Get pending bookings
                var reviewedBookingIds = allReviews
                    .Where(r => r.BookingID.HasValue)
                    .Select(r => r.BookingID.Value)
                    .ToList();

                var pendingBookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Where(b => b.ClientID == client.ClientID &&
                                b.Status == "Completed" &&
                                !reviewedBookingIds.Contains(b.BookingID))
                    .OrderByDescending(b => b.EventDate)
                    .ToListAsync();

                var pendingBookingViewModels = pendingBookings.Select(b => new PendingBookingViewModel
                {
                    BookingID = b.BookingID,
                    ServiceName = b.Service?.ServiceName ?? "N/A",
                    CompletedDate = b.EventDate
                }).ToList();

                _logger.LogInformation($"📋 Pending bookings: {pendingBookingViewModels.Count}");

                // ✅ STEP 4: Get unread messages
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) &&
                               !m.IsRead &&
                               m.SenderID != userId)
                    .CountAsync();

                // ✅ STEP 5: Build view model
                var viewModel = new ClientReviewsViewModel
                {
                    Client = client,
                    Reviews = reviewViewModels,
                    PendingBookings = pendingBookingViewModels,
                    UnreadMessages = unreadCount
                };

                _logger.LogInformation($"✅ Returning view with {viewModel.Reviews.Count} reviews and {viewModel.PendingBookings.Count} pending bookings");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading reviews");
                return View("Error", new ErrorViewModel { RequestId = "Error loading reviews" });
            }
        }

        // CREATE REVIEW - GET
        [Authorize(Roles = "Client")]
        [HttpGet("client/reviews/create")]  // ADD THIS ROUTE ATTRIBUTE
        public async Task<IActionResult> CreateReview(int bookingId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return NotFound("Client not found");
                }

                var booking = await _context.Bookings
                    .Include(b => b.Service)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId &&
                                              b.ClientID == client.ClientID &&
                                              b.Status == "Completed");

                if (booking == null)
                {
                    return NotFound("Booking not found or not completed");
                }

                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingID == bookingId);

                if (existingReview != null)
                {
                    return RedirectToAction("Reviews");
                }

                var viewModel = new CreateReviewViewModel
                {
                    BookingID = bookingId,
                    ServiceName = booking.Service?.ServiceName ?? "N/A"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create review page");
                return View("Error", new ErrorViewModel { RequestId = "Error loading review form" });
            }
        }

        [Authorize(Roles = "Client")]
        [HttpPost("client/reviews/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(CreateReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var bookingForError = await _context.Bookings
                        .Include(b => b.Service)
                        .FirstOrDefaultAsync(b => b.BookingID == model.BookingID);

                    if (bookingForError != null)
                        model.ServiceName = bookingForError.Service?.ServiceName ?? "N/A";

                    return View(model);
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                    return RedirectToAction("Login", "Account");

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                    return NotFound("Client not found");

                // Verify booking exists and belongs to this client
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == model.BookingID &&
                                              b.ClientID == client.ClientID &&
                                              b.Status == "Completed");

                if (booking == null)
                    return NotFound("Booking not found or not completed");

                // Check if review already exists
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.BookingID == model.BookingID);

                if (existingReview != null)
                {
                    TempData["ErrorMessage"] = "You have already reviewed this service.";
                    return RedirectToAction("Reviews", "Client"); // ✅ ADD "Client" controller
                }

                // ✅ CREATE REVIEW WITH CORRECT DATA
                var review = new Review
                {
                    BookingID = model.BookingID,
                    ReviewerID = userId,
                    RevieweeID = booking.ProviderID,
                    RevieweeType = booking.ProviderType,
                    Rating = model.Rating,
                    ReviewText = model.Comment,
                    IsApproved = true,
                    IsEdited = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Review created: ReviewID={review.ReviewID}, BookingID={booking.BookingID}, ReviewerID={userId}");

                // ✅ IMMEDIATELY update provider rating
                await UpdateProviderRating(booking.ProviderID, booking.ProviderType);

                return RedirectToAction("Reviews", "Client"); // ✅ EXPLICITLY specify controller
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                ModelState.AddModelError("", "An error occurred while submitting your review.");

                var bookingForError = await _context.Bookings
                    .Include(b => b.Service)
                    .FirstOrDefaultAsync(b => b.BookingID == model.BookingID);

                if (bookingForError != null)
                    model.ServiceName = bookingForError.Service?.ServiceName ?? "N/A";

                return View(model);
            }
        }

        // EDIT REVIEW - GET
        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> EditReview(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var review = await _context.Reviews
                    .Include(r => r.Booking)
                        .ThenInclude(b => b.Service)
                    .FirstOrDefaultAsync(r => r.ReviewID == id && r.ReviewerID == userId);

                if (review == null)
                {
                    return NotFound("Review not found");
                }

                var viewModel = new EditReviewViewModel
                {
                    ReviewID = review.ReviewID,
                    BookingID = (int)review.BookingID,
                    ServiceName = review.Booking?.Service?.ServiceName ?? "N/A",
                    Rating = review.Rating,
                    Comment = review.ReviewText ?? string.Empty
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit review page");
                return View("Error", new ErrorViewModel { RequestId = "Error loading review" });
            }
        }

        // EDIT REVIEW - POST
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReview(EditReviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var review = await _context.Reviews
                    .Include(r => r.Booking)
                    .FirstOrDefaultAsync(r => r.ReviewID == model.ReviewID && r.ReviewerID == userId);

                if (review == null)
                {
                    return NotFound("Review not found");
                }

                review.Rating = model.Rating;
                review.ReviewText = model.Comment;
                review.IsEdited = true;
                review.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Update provider's average rating
                await UpdateProviderRating(review.Booking.ProviderID, review.Booking.ProviderType);

                return RedirectToAction("Reviews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing review");
                return View("Error", new ErrorViewModel { RequestId = "Error editing review" });
            }
        }

        // SEND MESSAGE - POST
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int conversationId, string content)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new { success = false, message = "Message content cannot be empty" });
                }

                // Verify user is part of the conversation
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationID == conversationId &&
                                   cp.UserID == userId &&
                                   cp.LeftAt == null);

                if (!isParticipant)
                {
                    return Json(new { success = false, message = "Not authorized for this conversation" });
                }

                var message = new Message
                {
                    ConversationID = conversationId,
                    SenderID = userId,
                    MessageText = content,
                    MessageType = "Text",
                    SentAt = DateTime.Now,
                    IsRead = false
                };

                _context.Messages.Add(message);

                // Update conversation's last message time
                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation != null)
                {
                    conversation.LastMessageAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        public async Task<IActionResult> BookService([FromBody] BookServiceRequest request)
        {
            try
            {
                _logger.LogInformation($"BookService called with ServiceId: {request.ServiceId}, EventId: {request.EventId}");

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (client == null)
                {
                    return Json(new { success = false, message = "Client not found" });
                }

                var service = await _context.Services
                    .FirstOrDefaultAsync(s => s.ServiceID == request.ServiceId && s.IsActive && s.IsApproved);

                if (service == null)
                {
                    return Json(new { success = false, message = "Service not found or not available" });
                }

                var clientEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == request.EventId && e.ClientID == client.ClientID);

                if (clientEvent == null)
                {
                    return Json(new { success = false, message = "Event not found" });
                }

                // Check if booking already exists
                var existingBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.ServiceID == request.ServiceId &&
                                             b.ClientID == client.ClientID &&
                                             b.EventID == request.EventId);

                if (existingBooking != null)
                {
                    return Json(new { success = false, message = "Service already booked for this event" });
                }

                // Update the client's event status to "Pending" and associate with organizer
                if (service.ProviderType == "Organizer")
                {
                    clientEvent.OrganizerID = service.ProviderID;
                    clientEvent.Status = "Pending";
                    clientEvent.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Updated client event: EventID {clientEvent.EventID} - Associated with Organizer {service.ProviderID}");
                }

                // ========== USE BOOKING SERVICE INSTEAD ==========
                var booking = new Booking
                {
                    EventID = request.EventId,
                    ServiceID = request.ServiceId,
                    ClientID = client.ClientID,
                    ProviderID = service.ProviderID,
                    ProviderType = service.ProviderType,
                    EventDate = clientEvent.EventDate,
                    Status = "Pending",
                    QuoteAmount = service.Price
                };

                // Use BookingService to create booking - this will trigger the notification!
                var createdBooking = await _bookingService.CreateBookingAsync(booking);

                _logger.LogInformation($"Booking created successfully via BookingService: BookingID {createdBooking.BookingID}");

                return Json(new { success = true, message = "Booking created successfully", bookingId = createdBooking.BookingID });
                // ========== END FIX ==========
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking: {Message}", ex.Message);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Helper method to update provider ratings
        private async Task UpdateProviderRating(int providerId, string providerType)
        {
            try
            {
                _logger.LogInformation($"🔄 Updating rating for {providerType} {providerId}");

                // ✅ Get all approved reviews for this provider
                var providerReviews = await _context.Reviews
                    .Include(r => r.Booking)
                    .Where(r => r.Booking.ProviderID == providerId &&
                               r.Booking.ProviderType == providerType &&
                               r.IsApproved)
                    .ToListAsync();

                _logger.LogInformation($"📊 Found {providerReviews.Count} approved reviews for {providerType} {providerId}");

                // ✅ Calculate provider ratings
                if (!providerReviews.Any())
                {
                    // Reset ratings if no reviews
                    if (providerType == "Organizer")
                    {
                        var organizer = await _context.Organizers.FindAsync(providerId);
                        if (organizer != null)
                        {
                            organizer.AverageRating = 0;
                            organizer.TotalReviews = 0;
                            _logger.LogInformation($"✅ Reset Organizer {providerId} ratings to 0");
                        }
                    }
                    else if (providerType == "Supplier")
                    {
                        var supplier = await _context.Suppliers.FindAsync(providerId);
                        if (supplier != null)
                        {
                            supplier.AverageRating = 0;
                            supplier.TotalReviews = 0;
                            _logger.LogInformation($"✅ Reset Supplier {providerId} ratings to 0");
                        }
                    }
                }
                else
                {
                    var avgRating = providerReviews.Average(r => r.Rating);
                    var totalReviews = providerReviews.Count;

                    _logger.LogInformation($"📈 Calculated: Avg={avgRating:F2}, Total={totalReviews}");

                    if (providerType == "Organizer")
                    {
                        var organizer = await _context.Organizers.FindAsync(providerId);
                        if (organizer != null)
                        {
                            organizer.AverageRating = (decimal)avgRating;
                            organizer.TotalReviews = totalReviews;
                            _logger.LogInformation($"✅ Updated Organizer {providerId}: Rating={avgRating:F2}, Total={totalReviews}");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Organizer {providerId} not found");
                        }
                    }
                    else if (providerType == "Supplier")
                    {
                        var supplier = await _context.Suppliers.FindAsync(providerId);
                        if (supplier != null)
                        {
                            supplier.AverageRating = (decimal)avgRating;
                            supplier.TotalReviews = totalReviews;
                            _logger.LogInformation($"✅ Updated Supplier {providerId}: Rating={avgRating:F2}, Total={totalReviews}");
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Supplier {providerId} not found");
                        }
                    }
                }

                // ✅✅✅ NEW: ALSO UPDATE ALL SERVICES BY THIS PROVIDER ✅✅✅
                var services = await _context.Services
                    .Where(s => s.ProviderID == providerId && s.ProviderType == providerType)
                    .ToListAsync();

                _logger.LogInformation($"🔧 Updating {services.Count} services for {providerType} {providerId}");

                foreach (var service in services)
                {
                    // Get reviews specifically for this service
                    var serviceReviews = await _context.Reviews
                        .Include(r => r.Booking)
                        .Where(r => r.Booking.ServiceID == service.ServiceID && r.IsApproved)
                        .ToListAsync();

                    if (serviceReviews.Any())
                    {
                        service.AverageRating = (decimal)serviceReviews.Average(r => r.Rating);
                        service.TotalReviews = serviceReviews.Count;
                        _logger.LogInformation($"✅ Updated Service {service.ServiceID} ({service.ServiceName}): Rating={service.AverageRating:F2}, Reviews={service.TotalReviews}");
                    }
                    else
                    {
                        service.AverageRating = 0;
                        service.TotalReviews = 0;
                        _logger.LogInformation($"✅ Reset Service {service.ServiceID} ({service.ServiceName}) ratings to 0");
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"💾 All changes saved to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating ratings for {providerType} {providerId}");
            }
        }

        // Get all notifications for client
        [HttpGet("client/notifications")]
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
                _logger.LogError(ex, "Error getting notifications");
                return Json(new { success = false, message = "Error loading notifications" });
            }
        }

        // Get unread notification count
        [HttpGet("client/messages/unread-count")]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, count = 0 });
                }

                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                var unreadCount = await _context.Messages
                    .Where(m => conversationIds.Contains(m.ConversationID) &&
                               !m.IsRead &&
                               m.SenderID != userId)
                    .CountAsync();

                return Json(new { success = true, count = unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages count");
                return Json(new { success = false, count = 0 });
            }
        }

        // Mark notification as read
        [HttpPost("client/notifications/mark-read/{id}")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var success = await _notificationService.MarkAsReadAsync(id);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Json(new { success = false });
            }
        }

        // Mark all notifications as read
        [HttpPost("client/notifications/mark-all-read")]
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
                _logger.LogError(ex, "Error marking all as read");
                return Json(new { success = false });
            }
        }

        // Delete notification
        [HttpDelete("client/notifications/delete/{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var notification = await _context.Notifications.FindAsync(id);

                if (notification == null)
                {
                    return Json(new { success = false, message = "Notification not found" });
                }

                if (notification.UserID != userId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return Json(new { success = false, message = "Error deleting notification" });
            }
        }

        [Authorize(Roles = "Client")]
        [HttpGet("client/messages/{conversationId:int}")]
        public IActionResult MessagesWithConversation(int conversationId)
        {
            // Simply redirect to the existing Messages action with the query parameter
            return RedirectToAction("Messages", new { conversation = conversationId });
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
        [HttpDelete("client/messages/delete/{conversationId}")]
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

                _logger.LogInformation($"Conversation {conversationId} deleted successfully by User {userId}");

                return Json(new { success = true, message = "Conversation deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation");
                return Json(new { success = false, message = "Error deleting conversation" });
            }
        }
        [Authorize(Roles = "Client")]
        [HttpGet("client/reviews/diagnostic")]
        public async Task<IActionResult> ReviewDiagnostic()
        {
            try
            {
                var userId = GetCurrentUserId();

                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                // Get ALL reviews for this user (ignore IsApproved for diagnostic)
                var allReviews = await _context.Reviews
                    .Include(r => r.Booking)
                        .ThenInclude(b => b.Service)
                    .Include(r => r.Reviewer)
                    .Where(r => r.ReviewerID == userId)
                    .ToListAsync();

                // Get all bookings for this client
                var allBookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Where(b => b.ClientID == client.ClientID)
                    .ToListAsync();

                // Get organizer info for the booking you reviewed
                var organizerInfo = new List<object>();
                foreach (var review in allReviews)
                {
                    if (review.Booking != null)
                    {
                        var booking = await _context.Bookings.FindAsync(review.Booking.BookingID);
                        if (booking != null && booking.ProviderType == "Organizer")
                        {
                            var organizer = await _context.Organizers.FindAsync(booking.ProviderID);
                            if (organizer != null)
                            {
                                organizerInfo.Add(new
                                {
                                    organizerId = organizer.OrganizerID,
                                    businessName = organizer.BusinessName,
                                    avgRating = organizer.AverageRating,
                                    totalReviews = organizer.TotalReviews
                                });
                            }
                        }
                    }
                }

                var diagnostic = new
                {
                    userId = userId,
                    clientId = client?.ClientID,

                    totalReviewsFound = allReviews.Count,
                    reviews = allReviews.Select(r => new
                    {
                        reviewId = r.ReviewID,
                        bookingId = r.BookingID,
                        reviewerId = r.ReviewerID,
                        revieweeId = r.RevieweeID,
                        revieweeType = r.RevieweeType,
                        rating = r.Rating,
                        reviewText = r.ReviewText,
                        isApproved = r.IsApproved,
                        createdAt = r.CreatedAt,
                        serviceName = r.Booking?.Service?.ServiceName ?? "N/A",
                        bookingExists = r.Booking != null,
                        serviceExists = r.Booking?.Service != null
                    }).ToList(),

                    totalBookings = allBookings.Count,
                    completedBookings = allBookings.Count(b => b.Status == "Completed"),
                    bookings = allBookings.Select(b => new
                    {
                        bookingId = b.BookingID,
                        serviceName = b.Service?.ServiceName,
                        status = b.Status,
                        providerId = b.ProviderID,
                        providerType = b.ProviderType
                    }).ToList(),

                    organizers = organizerInfo
                };

                return Json(diagnostic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnostic error");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}