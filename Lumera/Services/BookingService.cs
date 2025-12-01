using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public BookingService(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<List<Booking>> GetClientBookingsAsync(int clientId)
        {
            return await _context.Bookings
                .Where(b => b.ClientID == clientId)
                .Include(b => b.Service)
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetProviderBookingsAsync(int providerId, string providerType)
        {
            return await _context.Bookings
                .Where(b => b.ProviderID == providerId && b.ProviderType == providerType)
                .Include(b => b.Service)
                .Include(b => b.Event)
                .Include(b => b.Client)
                .ThenInclude(c => c.User)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Service)
                .Include(b => b.Event)
                .Include(b => b.Client)
                .ThenInclude(c => c.User)
                .Include(b => b.Messages)
                .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId);
        }

        public async Task<Booking> CreateBookingAsync(Booking booking)
        {
            booking.BookingDate = DateTime.Now;
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            Console.WriteLine($"=== BOOKING CREATED ===");
            Console.WriteLine($"BookingID: {booking.BookingID}");
            Console.WriteLine($"ProviderType: {booking.ProviderType}");
            Console.WriteLine($"ProviderID: {booking.ProviderID}");
            Console.WriteLine($"ClientID: {booking.ClientID}");

            // Create notification for organizer
            if (booking.ProviderType == "Organizer")
            {
                var client = await _context.Clients
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.ClientID == booking.ClientID);

                Console.WriteLine($"Client found: {client != null}");
                Console.WriteLine($"Client.User found: {client?.User != null}");

                if (client?.User != null)
                {
                    Console.WriteLine($"Creating notification for Organizer UserID: {client.User.UserID}");
                    Console.WriteLine($"Client name: {client.User.FirstName} {client.User.LastName}");

                    await _notificationService.CreateBookingNotificationAsync(
                        booking.ProviderID,
                        booking.BookingID,
                        $"{client.User.FirstName} {client.User.LastName}"
                    );

                    Console.WriteLine("Notification created successfully!");
                }
                else
                {
                    Console.WriteLine("ERROR: Client or Client.User is null!");
                }
            }

            return booking;
        }

        public async Task<bool> UpdateBookingAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var booking = await GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            booking.Status = status;
            var result = await UpdateBookingAsync(booking);

            // ========== FIXED: Send notification to CLIENT, not organizer ==========
            if (result && booking.Client?.UserID != null)
            {
                // Get organizer information
                var organizer = await _context.Organizers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrganizerID == booking.ProviderID);

                if (organizer != null)
                {
                    var organizerName = organizer.BusinessName ??
                                       $"{organizer.User?.FirstName} {organizer.User?.LastName}";

                    // Send notification to CLIENT (not organizer)
                    await _notificationService.CreateClientBookingStatusNotificationAsync(
                        clientUserId: (int)booking.Client.UserID,
                        bookingId: booking.BookingID,
                        status: status,
                        organizerName: organizerName
                    );

                    Console.WriteLine($"Booking status notification sent to Client UserID: {booking.Client.UserID}");
                }
            }

            return result;
        }

        public async Task<bool> DeleteBookingAsync(int bookingId)
        {
            var booking = await GetBookingByIdAsync(bookingId);
            if (booking == null) return false;

            _context.Bookings.Remove(booking);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<Booking>> GetBookingsByEventAsync(int eventId)
        {
            return await _context.Bookings
                .Where(b => b.EventID == eventId)
                .Include(b => b.Service)
                .ToListAsync();
        }

        public async Task<bool> AddBookingMessageAsync(BookingMessage message)
        {
            message.SentAt = DateTime.Now;
            _context.BookingMessages.Add(message);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<BookingMessage>> GetBookingMessagesAsync(int bookingId)
        {
            return await _context.BookingMessages
                .Where(m => m.BookingID == bookingId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}