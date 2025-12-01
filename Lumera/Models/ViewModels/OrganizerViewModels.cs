namespace Lumera.Models
{
    public class OrganizerListViewModel
    {
        public List<OrganizerViewModel> Organizers { get; set; } = new List<OrganizerViewModel>();
        public string? SearchQuery { get; set; }
        public string? SelectedSpecialty { get; set; }
        public List<string> Specialties { get; set; } = new List<string>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }

    public class OrganizerViewModel
    {
        public int OrganizerID { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessDescription { get; set; } = string.Empty;
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? ServiceAreas { get; set; }
        public string? Location { get; set; }
        public string? Specialty { get; set; }
        public string? ImageURL { get; set; }
    }

    // Find your OrganizerDashboardViewModel class and add this property:

    public class OrganizerDashboardViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();  // Add default initialization
        public int ActiveEvents { get; set; }
        public int PendingBookings { get; set; }
        public decimal MonthlyEarnings { get; set; } = 0; // Add default value
        public int UnreadMessages { get; set; }
        public int UnreadNotifications { get; set; }
        public List<Event> UpcomingEvents { get; set; } = new List<Event>(); // Add default initialization
        public List<Booking> RecentBookings { get; set; } = new List<Booking>(); // Add default initialization
        public List<Message> RecentMessages { get; set; } = new List<Message>(); // Add default initialization
    }

    public class OrganizerEventsViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public List<Event> Events { get; set; } = new List<Event>();
        public int UnreadMessages { get; set; }
    }

    public class UpdateEventStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class OrganizerProfileViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public int CompletedEvents { get; set; }
        public int UnreadMessages { get; set; }
    }

    public class OrganizerBookingsViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public int UnreadMessages { get; set; }
    }

    public class OrganizerServicesViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public List<Service> Services { get; set; } = new List<Service>();
        public int UnreadMessages { get; set; }
    }

    public class OrganizerEarningsViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public decimal TotalEarnings { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal MonthlyEarnings { get; set; }
        public decimal PendingPayouts { get; set; }
        public decimal NextPayoutAmount { get; set; }
        public DateTime NextPayoutDate { get; set; }
        public int PayoutProgress { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public int UnreadMessages { get; set; }
    }

    public class OrganizerMessagesViewModel
    {
        public Organizer Organizer { get; set; } = new Organizer();
        public int UnreadMessages { get; set; }
        public List<Conversation> Conversations { get; set; } = new List<Conversation>();
        public List<Message> Messages { get; set; } = new List<Message>();
        public int? SelectedConversationId { get; set; }
    }
}