using Lumera.Data;
using Lumera.Models;
using Microsoft.EntityFrameworkCore;

namespace Lumera.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetProviderReviewsAsync(int providerId, string providerType)
        {
            // Only show approved reviews publicly
            return await _context.Reviews
                .Where(r => r.RevieweeID == providerId &&
                           r.RevieweeType == providerType &&
                           r.IsApproved)
                .Include(r => r.Reviewer)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Service)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetReviewByIdAsync(int reviewId)
        {
            return await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Service)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            review.CreatedAt = DateTime.Now;
            review.UpdatedAt = DateTime.Now;
            review.IsApproved = true;  // Auto-approve

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Update provider rating
            await UpdateProviderRatingAsync(review.RevieweeID, review.RevieweeType);

            return review;
        }

        public async Task<bool> UpdateReviewAsync(Review review)
        {
            review.UpdatedAt = DateTime.Now;
            review.IsEdited = true;
            _context.Reviews.Update(review);

            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                await UpdateProviderRatingAsync(review.RevieweeID, review.RevieweeType);
            }

            return result;
        }

        public async Task<bool> DeleteReviewAsync(int reviewId)
        {
            var review = await GetReviewByIdAsync(reviewId);
            if (review == null) return false;

            _context.Reviews.Remove(review);
            var result = await _context.SaveChangesAsync() > 0;

            if (result)
            {
                await UpdateProviderRatingAsync(review.RevieweeID, review.RevieweeType);
            }

            return result;
        }

        public async Task<bool> ApproveReviewAsync(int reviewId)
        {
            var review = await GetReviewByIdAsync(reviewId);
            if (review == null) return false;

            review.IsApproved = true;
            return await UpdateReviewAsync(review);
        }

        public async Task<List<Review>> GetPendingReviewsAsync()
        {
            // Return unapproved reviews for admin moderation
            return await _context.Reviews
                .Where(r => !r.IsApproved)
                .Include(r => r.Reviewer)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Service)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetBookingReviewAsync(int bookingId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookingID == bookingId);
        }

        public async Task<bool> UpdateProviderRatingAsync(int providerId, string providerType)
        {
            // Only include approved reviews in rating calculation
            var reviews = await _context.Reviews
                .Where(r => r.RevieweeID == providerId &&
                           r.RevieweeType == providerType &&
                           r.IsApproved)
                .ToListAsync();

            if (!reviews.Any()) return true;

            var averageRating = reviews.Average(r => r.Rating);
            var totalReviews = reviews.Count;

            if (providerType == "Organizer")
            {
                var organizer = await _context.Organizers.FindAsync(providerId);
                if (organizer != null)
                {
                    organizer.AverageRating = (decimal)averageRating;
                    organizer.TotalReviews = totalReviews;
                }
            }
            else if (providerType == "Supplier")
            {
                var supplier = await _context.Suppliers.FindAsync(providerId);
                if (supplier != null)
                {
                    supplier.AverageRating = (decimal)averageRating;
                    supplier.TotalReviews = totalReviews;
                }
            }

            return await _context.SaveChangesAsync() > 0;
        }

        // Get all reviews (for admin)
        public async Task<List<Review>> GetAllReviewsAsync()
        {
            return await _context.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Booking)
                .ThenInclude(b => b.Service)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}