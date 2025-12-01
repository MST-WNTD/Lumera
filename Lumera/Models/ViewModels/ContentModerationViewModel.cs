namespace Lumera.Models.AdminViewModels
{
    public class ContentModerationViewModel
    {
        public List<PendingServiceViewModel> PendingServices { get; set; } = new List<PendingServiceViewModel>();
        public List<PendingReviewViewModel> PendingReviews { get; set; } = new List<PendingReviewViewModel>();
        public List<string> ServiceCategories { get; set; } = new List<string>();
        public int PendingServicesCount { get; set; }
        public int FlaggedReviewsCount { get; set; }
        public int ReportedUsersCount { get; set; }
        public int PendingEventsCount { get; set; }
    }

    public class PendingServiceViewModel
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
    public class PendingReviewViewModel
    {
        public int ReviewID { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string ReviewText { get; set; } = string.Empty;
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Approved";
    }
}
