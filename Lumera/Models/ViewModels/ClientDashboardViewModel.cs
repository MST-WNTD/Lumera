using System.ComponentModel.DataAnnotations;

namespace Lumera.Models.ViewModels
{
    public class ClientDashboardViewModel
    {
        public Client Client { get; set; } = new Client();
        public List<Event> UpcomingEvents { get; set; } = new List<Event>();
        public List<BookingViewModel> RecentBookings { get; set; } = new List<BookingViewModel>();
        public List<MessageViewModel> RecentMessages { get; set; } = new List<MessageViewModel>();
        public List<ReviewViewModel> RecentReviews { get; set; } = new List<ReviewViewModel>();
        public int UnreadMessages { get; set; }
    }

    public class BookingViewModel
    {
        public int BookingID { get; set; }
        public Service? Service { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime EventDate { get; set; }
    }

    public class MessageViewModel
    {
        public int MessageID { get; set; }
        public string SenderType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class ReviewViewModel
    {
        public int ReviewID { get; set; }
        public Booking? Booking { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateReviewViewModel
    {
        public int BookingID { get; set; }
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 1000 characters")]
        public string Comment { get; set; } = string.Empty;
    }

    public class EditReviewViewModel
    {
        public int ReviewID { get; set; }
        public int BookingID { get; set; }
        public string ServiceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 1000 characters")]
        public string Comment { get; set; } = string.Empty;
    }
}