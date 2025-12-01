using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    // Dashboard ViewModels
    public class SupplierDashboardViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public int TotalBookings { get; set; }
        public int ActiveServices { get; set; }
        public int PendingRequests { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<SupplierBookingViewModel> RecentBookings { get; set; } = new List<SupplierBookingViewModel>();
    }

    // Fix the BookingViewModel conflict by using different names
    public class SupplierBookingViewModel
    {
        public int BookingID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal QuoteAmount { get; set; }
        public decimal? FinalAmount { get; set; }
    }

    // Events ViewModels
    public class SupplierEventsViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public List<SupplierEventViewModel> Events { get; set; } = new List<SupplierEventViewModel>();
    }

    public class SupplierEventViewModel
    {
        public int BookingID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? GuestCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string? ClientPhone { get; set; }
        public decimal QuoteAmount { get; set; }
        public decimal? FinalAmount { get; set; }
    }

    // Bookings ViewModels
    public class SupplierBookingsViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public List<SupplierBookingDetailViewModel> Bookings { get; set; } = new List<SupplierBookingDetailViewModel>();
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class SupplierBookingDetailViewModel
    {
        public int BookingID { get; set; }
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public int ClientID { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string? ClientPhone { get; set; }
        public int? EventID { get; set; }
        public string? EventName { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal QuoteAmount { get; set; }
        public decimal? FinalAmount { get; set; }
        public string? ClientNotes { get; set; }
        public string? ProviderNotes { get; set; }
        public string? ServiceDetails { get; set; }
    }

    // Services ViewModels
    public class SupplierServicesViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public List<SupplierServiceViewModel> Services { get; set; } = new List<SupplierServiceViewModel>();
    }

    // Fix the ServiceViewModel conflict by using different names
    public class SupplierServiceViewModel
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? ServiceDescription { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal? BasePrice { get; set; }
        public decimal Price { get; set; }
        public string? PriceType { get; set; }
        public string? Location { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<ServiceGallery> Gallery { get; set; } = new List<ServiceGallery>();
        public DateTime CreatedAt { get; set; }
    }

    public class ServiceCreateViewModel
    {
        [Required]
        [StringLength(255)]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        public string ServiceDescription { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? BasePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [StringLength(50)]
        public string? PriceType { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class SupplierConversationViewModel
    {
        public int ConversationID { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public List<UserViewModel> OtherParticipants { get; set; } = new List<UserViewModel>();
    }

    public class UserViewModel
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
    }

    // Profile ViewModels
    public class SupplierProfileViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    // Earnings ViewModels
    public class SupplierEarningsViewModel
    {
        public Supplier Supplier { get; set; } = null!;
        public decimal TotalEarnings { get; set; }
        public decimal AvailableForPayout { get; set; }
        public decimal PendingClearance { get; set; }
        public int CompletedBookings { get; set; }
        public List<TransactionViewModel> Transactions { get; set; } = new List<TransactionViewModel>();
        public List<PayoutViewModel> Payouts { get; set; } = new List<PayoutViewModel>();
    }

    public class TransactionViewModel
    {
        public int TransactionID { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string PayerName { get; set; } = string.Empty;
    }

    public class PayoutViewModel
    {
        public int PayoutID { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PayoutMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
    public class SupplierMessagesViewModel
    {
        public Supplier Supplier { get; set; }
        public int UnreadMessages { get; set; }
        public List<Conversation> Conversations { get; set; }
        public List<Message> Messages { get; set; }
    }
}