using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lumera.Data;
using Lumera.Models;
using System.Security.Claims;
using Lumera.Models.AdminViewModels;

namespace Lumera.Controllers
{
    [Authorize(Roles = "Supplier")]
    public class SupplierController : Controller
    {
        private readonly ILogger<SupplierController> _logger;
        private readonly ApplicationDbContext _context;

        public SupplierController(ILogger<SupplierController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // In SupplierController - Update the Dashboard method
        // In SupplierController - Update the Dashboard method
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .Include(s => s.Services)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                // Calculate dashboard statistics
                var totalBookings = await _context.Bookings
                    .CountAsync(b => b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID);

                var pendingRequests = await _context.Bookings
                    .CountAsync(b => b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID && b.Status == "Pending");

                var activeServices = supplier.Services.Count(s => s.IsActive);

                var recentBookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .Where(b => b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID)
                    .OrderByDescending(b => b.BookingDate)
                    .Take(5)
                    .ToListAsync();

                var viewModel = new SupplierDashboardViewModel
                {
                    Supplier = supplier,
                    TotalBookings = totalBookings,
                    ActiveServices = activeServices,
                    PendingRequests = pendingRequests,
                    AverageRating = supplier.AverageRating,
                    TotalReviews = supplier.TotalReviews,
                    RecentBookings = recentBookings.Select(b => new SupplierBookingViewModel
                    {
                        BookingID = b.BookingID,
                        ServiceName = b.Service?.ServiceName ?? "Unknown Service",
                        ClientName = $"{b.Client?.User?.FirstName} {b.Client?.User?.LastName}",
                        EventDate = b.EventDate,
                        Status = b.Status,
                        QuoteAmount = b.QuoteAmount ?? 0,
                        FinalAmount = b.FinalAmount
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier dashboard");
                return View("Error");
            }
        }

        public async Task<IActionResult> MyEvents()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                var events = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Service)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .Where(b => b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID && b.Event != null)
                    .Select(b => new SupplierEventViewModel
                    {
                        BookingID = b.BookingID,
                        EventID = b.Event.EventID,
                        EventName = b.Event.EventName,
                        EventType = b.Event.EventType,
                        EventDate = b.Event.EventDate,
                        Location = b.Event.Location,
                        GuestCount = b.Event.GuestCount,
                        Status = b.Status,
                        ServiceName = b.Service.ServiceName,
                        ClientName = $"{b.Client.User.FirstName} {b.Client.User.LastName}",
                        ClientEmail = b.Client.User.Email,
                        ClientPhone = b.Client.User.Phone,
                        QuoteAmount = b.QuoteAmount ?? 0,
                        FinalAmount = b.FinalAmount
                    })
                    .OrderBy(e => e.EventDate)
                    .ToListAsync();

                var viewModel = new SupplierEventsViewModel
                {
                    Supplier = supplier,
                    Events = events
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier events");
                return View("Error");
            }
        }

        public async Task<IActionResult> Bookings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                var bookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .Include(b => b.Event)
                    .Where(b => b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID)
                    .Select(b => new SupplierBookingDetailViewModel
                    {
                        BookingID = b.BookingID,
                        ServiceID = b.ServiceID ?? 0,
                        ServiceName = b.Service.ServiceName,
                        ClientID = b.ClientID ?? 0,
                        ClientName = $"{b.Client.User.FirstName} {b.Client.User.LastName}",
                        ClientEmail = b.Client.User.Email,
                        ClientPhone = b.Client.User.Phone,
                        EventID = b.EventID,
                        EventName = b.Event.EventName,
                        EventDate = b.EventDate,
                        BookingDate = b.BookingDate,
                        Status = b.Status,
                        QuoteAmount = b.QuoteAmount ?? 0,
                        FinalAmount = b.FinalAmount,
                        ClientNotes = b.ClientNotes,
                        ProviderNotes = b.ProviderNotes,
                        ServiceDetails = b.ServiceDetails
                    })
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                var viewModel = new SupplierBookingsViewModel
                {
                    Supplier = supplier,
                    Bookings = bookings,
                    PendingCount = bookings.Count(b => b.Status == "Pending"),
                    ConfirmedCount = bookings.Count(b => b.Status == "Confirmed"),
                    CompletedCount = bookings.Count(b => b.Status == "Completed")
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier bookings");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status, string? providerNotes = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserID == userId);
                if (supplier == null)
                {
                    return Json(new { success = false, message = "Supplier not found" });
                }

                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && b.ProviderType == "Supplier" && b.ProviderID == supplier.SupplierID);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found" });
                }

                booking.Status = status;
                if (!string.IsNullOrEmpty(providerNotes))
                {
                    booking.ProviderNotes = providerNotes;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Booking status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating booking status");
                return Json(new { success = false, message = "Error updating booking status" });
            }
        }

        public async Task<IActionResult> MyServices()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .Include(s => s.Services)
                        .ThenInclude(svc => svc.Gallery)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                var viewModel = new SupplierServicesViewModel
                {
                    Supplier = supplier,
                    Services = supplier.Services.Select(s => new SupplierServiceViewModel
                    {
                        ServiceID = s.ServiceID,
                        ServiceName = s.ServiceName,
                        ServiceDescription = s.ServiceDescription,
                        Category = s.Category,
                        BasePrice = s.BasePrice,
                        Price = s.Price,
                        PriceType = s.PriceType,
                        Location = s.Location,
                        IsActive = s.IsActive,
                        IsApproved = s.IsApproved,
                        AverageRating = s.AverageRating,
                        TotalReviews = s.TotalReviews,
                        Gallery = s.Gallery.ToList(),
                        CreatedAt = s.CreatedAt
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier services");
                return View("Error");
            }
        }

        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(ServiceCreateViewModel model)
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

                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserID == userId);
                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                var service = new Service
                {
                    ProviderID = supplier.SupplierID,
                    ProviderType = "Supplier",
                    ServiceName = model.ServiceName,
                    ServiceDescription = model.ServiceDescription,
                    Category = model.Category,
                    BasePrice = model.BasePrice,
                    Price = model.Price,
                    PriceType = model.PriceType,
                    Location = model.Location,
                    IsActive = true,
                    IsApproved = false,
                    CreatedAt = DateTime.Now
                };

                _context.Services.Add(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Service created successfully! It will be visible after admin approval.";
                return RedirectToAction("MyServices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                ModelState.AddModelError("", "Error creating service. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
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

        public class ToggleStatusRequest
        {
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> MyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                var viewModel = new SupplierProfileViewModel
                {
                    Supplier = supplier,
                    User = supplier.User
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier profile");
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SupplierProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("MyProfile", model);
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                // Update supplier properties
                supplier.BusinessName = model.Supplier.BusinessName;
                supplier.BusinessDescription = model.Supplier.BusinessDescription;
                supplier.ServiceCategory = model.Supplier.ServiceCategory;
                supplier.ServiceAreas = model.Supplier.ServiceAreas;
                supplier.YearsOfExperience = model.Supplier.YearsOfExperience;

                // Update user properties
                supplier.User.FirstName = model.User.FirstName;
                supplier.User.LastName = model.User.LastName;
                supplier.User.Phone = model.User.Phone;
                supplier.User.AvatarURL = model.User.AvatarURL;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("MyProfile");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier profile");
                ModelState.AddModelError("", "Error updating profile. Please try again.");
                return View("MyProfile", model);
            }
        }

        public async Task<IActionResult> Earnings()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                // Get transactions for supplier
                var transactions = await _context.Transactions
                    .Include(t => t.Booking)
                        .ThenInclude(b => b.Service)
                    .Include(t => t.Payer)
                    .Where(t => t.PayeeID == userId && t.Status == "Completed")
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();

                // Get payouts for supplier
                var payouts = await _context.Payouts
                    .Where(p => p.PayeeID == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var totalEarnings = transactions.Sum(t => t.Amount);
                var availableForPayout = totalEarnings - payouts.Where(p => p.Status == "Completed").Sum(p => p.Amount);
                var pendingClearance = transactions.Where(t => t.TransactionDate >= DateTime.Now.AddDays(-3)).Sum(t => t.Amount);
                var completedBookings = transactions.Count;

                var viewModel = new SupplierEarningsViewModel
                {
                    Supplier = supplier,
                    TotalEarnings = totalEarnings,
                    AvailableForPayout = availableForPayout,
                    PendingClearance = pendingClearance,
                    CompletedBookings = completedBookings,
                    Transactions = transactions.Select(t => new TransactionViewModel
                    {
                        TransactionID = t.TransactionID,
                        Amount = t.Amount,
                        Description = t.Description,
                        Type = "Earning",
                        Status = t.Status,
                        Date = t.TransactionDate,
                        ServiceName = t.Booking?.Service?.ServiceName ?? "Unknown Service",
                        PayerName = $"{t.Payer?.FirstName} {t.Payer?.LastName}"
                    }).ToList(),
                    Payouts = payouts.Select(p => new PayoutViewModel
                    {
                        PayoutID = p.PayoutID,
                        Amount = p.Amount,
                        Status = p.Status,
                        PayoutMethod = p.PayoutMethod,
                        CreatedAt = p.CreatedAt,
                        ProcessedAt = p.ProcessedAt
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier earnings");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RequestPayout(decimal amount, string payoutMethod)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Validate available balance
                var transactions = await _context.Transactions
                    .Where(t => t.PayeeID == userId && t.Status == "Completed")
                    .ToListAsync();

                var payouts = await _context.Payouts
                    .Where(p => p.PayeeID == userId && p.Status != "Failed")
                    .ToListAsync();

                var totalEarnings = transactions.Sum(t => t.Amount);
                var totalPayouts = payouts.Sum(p => p.Amount);
                var availableBalance = totalEarnings - totalPayouts;

                if (amount > availableBalance)
                {
                    return Json(new { success = false, message = "Requested amount exceeds available balance" });
                }

                var payout = new Payout
                {
                    PayeeID = userId,
                    Amount = amount,
                    PayoutMethod = payoutMethod,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Payouts.Add(payout);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payout request submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting payout");
                return Json(new { success = false, message = "Error requesting payout" });
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

        // Add these methods to your SupplierController.cs

        // MESSAGES PAGE
        [HttpGet]
        public async Task<IActionResult> Messages(int? conversation = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return NotFound("Supplier not found");
                }

                // Get all conversation IDs where the supplier is a participant
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserID == userId && cp.LeftAt == null)
                    .Select(cp => cp.ConversationID)
                    .ToListAsync();

                // Get conversations with all related data properly loaded
                var conversations = await _context.Conversations
                    .Include(c => c.Event)
                        .ThenInclude(e => e.Client)
                            .ThenInclude(c => c.User)
                    .Include(c => c.Messages)
                        .ThenInclude(m => m.Sender)
                    .Where(c => conversationIds.Contains(c.ConversationID))
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

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

                var unreadMessages = await GetUnreadMessagesCount(userId);

                var viewModel = new SupplierMessagesViewModel
                {
                    Supplier = supplier,
                    UnreadMessages = unreadMessages,
                    Conversations = conversations,
                    Messages = messages
                };

                ViewBag.SelectedConversationId = selectedConversationId;

                return View("Messages", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier messages");
                return View("Error");
            }
        }

        // API endpoint to get messages for a specific conversation
        [HttpGet("supplier/messages/conversation/{conversationId}")]
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
                _logger.LogError(ex, "Error loading conversation messages");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }

        // API endpoint to send a message
        [HttpPost("supplier/messages/send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
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

                // Update conversation last message time
                var conversation = await _context.Conversations.FindAsync(request.ConversationId);
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

        // API endpoint to start a conversation from a booking
        [HttpPost("supplier/messages/start-conversation")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var supplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.UserID == userId);

                if (supplier == null)
                {
                    return Json(new { success = false, message = "Supplier not found" });
                }

                // Get the booking with related data
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                    .Include(b => b.Client)
                        .ThenInclude(c => c.User)
                    .FirstOrDefaultAsync(b => b.BookingID == request.BookingId &&
                                             b.ProviderID == supplier.SupplierID &&
                                             b.ProviderType == "Supplier");

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found" });
                }

                if (booking.Client?.User == null)
                {
                    return Json(new { success = false, message = "Client information not found" });
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

                // Create new conversation
                var conversation = new Conversation
                {
                    EventID = booking.EventID,
                    CreatedAt = DateTime.Now,
                    LastMessageAt = DateTime.Now
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                // Add participants (supplier and client)
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

                return Json(new { success = true, conversationId = conversation.ConversationID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return Json(new { success = false, message = "Error starting conversation" });
            }
        }

        // Helper method to get unread messages count
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
    }
}