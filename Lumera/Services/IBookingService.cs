using Lumera.Models;

namespace Lumera.Services
{
    public interface IBookingService
    {
        Task<List<Booking>> GetClientBookingsAsync(int clientId);
        Task<List<Booking>> GetProviderBookingsAsync(int providerId, string providerType);
        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task<Booking> CreateBookingAsync(Booking booking);
        Task<bool> UpdateBookingAsync(Booking booking);
        Task<bool> UpdateBookingStatusAsync(int bookingId, string status);
        Task<bool> DeleteBookingAsync(int bookingId);
        Task<List<Booking>> GetBookingsByEventAsync(int eventId);
        Task<bool> AddBookingMessageAsync(BookingMessage message);
        Task<List<BookingMessage>> GetBookingMessagesAsync(int bookingId);
    }
}