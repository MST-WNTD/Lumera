using Lumera.Models;

namespace Lumera.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, string title, string message,
            string notificationType, int? referenceId = null, string? referenceType = null,
            string? redirectUrl = null);

        Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 10);

        Task<List<Notification>> GetUnreadNotificationsAsync(int userId);

        Task<int> GetUnreadCountAsync(int userId);

        Task<bool> MarkAsReadAsync(int notificationId);

        Task<bool> MarkAllAsReadAsync(int userId);

        Task<bool> DeleteNotificationAsync(int notificationId);

        Task CreateBookingNotificationAsync(int organizerId, int bookingId, string clientName);

        Task CreateMessageNotificationAsync(int userId, int conversationId, string senderName);

        Task CreateBookingStatusNotificationAsync(int organizerId, int bookingId, string status, string clientName);
        Task CreateClientBookingStatusNotificationAsync(int clientUserId, int bookingId, string status, string organizerName);
    }
}