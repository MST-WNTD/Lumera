using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification> CreateNotificationAsync(int userId, string title, string message,
            string notificationType, int? referenceId = null, string? referenceType = null,
            string? redirectUrl = null)
        {
            var notification = new Notification
            {
                UserID = userId,
                Title = title,
                Message = message,
                NotificationType = notificationType,
                ReferenceID = referenceId,
                ReferenceType = referenceType,
                RedirectUrl = redirectUrl,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserID == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task CreateBookingNotificationAsync(int organizerId, int bookingId, string clientName)
        {
            Console.WriteLine($"=== CreateBookingNotificationAsync Called ===");
            Console.WriteLine($"OrganizerID: {organizerId}");
            Console.WriteLine($"BookingID: {bookingId}");
            Console.WriteLine($"ClientName: {clientName}");

            // Get organizer's userId
            var organizer = await _context.Organizers.FindAsync(organizerId);

            Console.WriteLine($"Organizer found: {organizer != null}");
            Console.WriteLine($"Organizer.UserID: {organizer?.UserID}");

            if (organizer == null)
            {
                Console.WriteLine("ERROR: Organizer not found!");
                return;
            }

            if (organizer.UserID == null)
            {
                Console.WriteLine("ERROR: Organizer.UserID is null!");
                return;
            }

            Console.WriteLine($"Creating notification for UserID: {organizer.UserID}");

            await CreateNotificationAsync(
                userId: (int)organizer.UserID,
                title: "New Booking Request",
                message: $"{clientName} has sent you a booking request",
                notificationType: "Booking",
                referenceId: bookingId,
                referenceType: "Booking",
                redirectUrl: $"/organizer/bookings/details/{bookingId}"
            );

            Console.WriteLine("Booking notification created successfully!");
        }

        public async Task CreateMessageNotificationAsync(int userId, int conversationId, string senderName)
        {
            // Get the user to determine their role
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"ERROR: User not found for UserID: {userId}");
                return;
            }

            // Determine redirect URL based on user's role
            string redirectUrl = user.Role switch
            {
                "Client" => $"/client/messages?conversation={conversationId}",
                "Organizer" => $"/organizer/messages?conversation={conversationId}",
                "Supplier" => $"/supplier/messages?conversation={conversationId}",
                _ => $"/messages?conversation={conversationId}"
            };

            Console.WriteLine($"Creating message notification for UserID: {userId}, Role: {user.Role}, RedirectUrl: {redirectUrl}");

            // ? Check for ANY notification (read or unread) for this conversation
            var existingNotification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.UserID == userId &&
                    n.NotificationType == "Message" &&
                    n.ReferenceID == conversationId &&
                    n.ReferenceType == "Conversation");

            if (existingNotification != null)
            {
                // Count unread messages in this conversation
                var unreadCount = await _context.Messages
                    .Where(m => m.ConversationID == conversationId &&
                               !m.IsRead &&
                               m.SenderID != userId)
                    .CountAsync();

                // ? Reactivate the notification (mark as unread)
                existingNotification.Title = "New Message";
                existingNotification.Message = unreadCount > 1
                    ? $"You have {unreadCount} new messages from {senderName}"
                    : $"You have a new message from {senderName}";
                existingNotification.IsRead = false;  // ? Mark as UNREAD (reactivate!)
                existingNotification.ReadAt = null;    // ? Clear read timestamp
                existingNotification.CreatedAt = DateTime.Now;  // Update timestamp
                existingNotification.RedirectUrl = redirectUrl;

                await _context.SaveChangesAsync();

                Console.WriteLine($"Reactivated notification for conversation {conversationId} - {unreadCount} unread messages");
                return;
            }

            // Only create new notification if none exists
            await CreateNotificationAsync(
                userId: userId,
                title: "New Message",
                message: $"You have a new message from {senderName}",
                notificationType: "Message",
                referenceId: conversationId,
                referenceType: "Conversation",
                redirectUrl: redirectUrl
            );

            Console.WriteLine($"Created new notification for conversation {conversationId}");
        }

        public async Task CreateBookingStatusNotificationAsync(int organizerId, int bookingId, string status, string clientName)
        {
            // Get organizer's userId
            var organizer = await _context.Organizers.FindAsync(organizerId);
            if (organizer == null) return;

            string title = status switch
            {
                "Confirmed" => "Booking Confirmed",
                "Completed" => "Booking Completed",
                "Cancelled" => "Booking Cancelled",
                _ => "Booking Status Updated"
            };

            string message = status switch
            {
                "Confirmed" => $"Your booking with {clientName} has been confirmed",
                "Completed" => $"Your booking with {clientName} has been marked as completed",
                "Cancelled" => $"Your booking with {clientName} has been cancelled",
                _ => $"Booking status with {clientName} has been updated to {status}"
            };

            await CreateNotificationAsync(
                userId: (int)organizer.UserID,
                title: title,
                message: message,
                notificationType: "Booking",
                referenceId: bookingId,
                referenceType: "Booking",
                redirectUrl: $"/organizer/bookings/details/{bookingId}"
            );
        }

        public async Task CreateClientBookingStatusNotificationAsync(int clientUserId, int bookingId, string status, string organizerName)
        {
            Console.WriteLine($"=== CreateClientBookingStatusNotificationAsync Called ===");
            Console.WriteLine($"ClientUserID: {clientUserId}");
            Console.WriteLine($"BookingID: {bookingId}");
            Console.WriteLine($"Status: {status}");
            Console.WriteLine($"OrganizerName: {organizerName}");

            string title = status switch
            {
                "Confirmed" => "Booking Confirmed",
                "Completed" => "Booking Completed",
                "Cancelled" => "Booking Cancelled",
                "Rejected" => "Booking Rejected",
                _ => "Booking Status Updated"
            };

            string message = status switch
            {
                "Confirmed" => $"{organizerName} has confirmed your booking!",
                "Completed" => $"Your booking with {organizerName} has been completed",
                "Cancelled" => $"Your booking with {organizerName} has been cancelled",
                "Rejected" => $"{organizerName} has declined your booking request",
                _ => $"Your booking status with {organizerName} has been updated to {status}"
            };

            await CreateNotificationAsync(
                userId: clientUserId,
                title: title,
                message: message,
                notificationType: "Booking",
                referenceId: bookingId,
                referenceType: "Booking",
                redirectUrl: $"/client/bookings"
            );

            Console.WriteLine("Client booking notification created successfully!");
        }
    }
}