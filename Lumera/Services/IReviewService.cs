using Lumera.Models;

namespace Lumera.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetProviderReviewsAsync(int providerId, string providerType);
        Task<Review?> GetReviewByIdAsync(int reviewId);
        Task<Review> CreateReviewAsync(Review review);
        Task<bool> UpdateReviewAsync(Review review);
        Task<bool> DeleteReviewAsync(int reviewId);
        Task<bool> ApproveReviewAsync(int reviewId);
        Task<List<Review>> GetPendingReviewsAsync();
        Task<Review?> GetBookingReviewAsync(int bookingId);
        Task<bool> UpdateProviderRatingAsync(int providerId, string providerType);
        Task<List<Review>> GetAllReviewsAsync();
    }
}