namespace Lumera.Models
{
    public class ClientReviewsViewModel
    {
        public Client Client { get; set; } = new Client();
        public List<ReviewDetailViewModel> Reviews { get; set; } = new List<ReviewDetailViewModel>();
        public List<PendingBookingViewModel> PendingBookings { get; set; } = new List<PendingBookingViewModel>();
        public int UnreadMessages { get; set; }
    }

    public class ReviewDetailViewModel
    {
        public int ReviewID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PendingBookingViewModel
    {
        public int BookingID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
    }
}