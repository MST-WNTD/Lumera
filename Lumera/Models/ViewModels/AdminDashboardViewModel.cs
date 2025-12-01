namespace Lumera.Models.AdminViewModels
{
    public class AdminDashboardViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new List<AdminUserViewModel>();
        public List<ContentModerationItem> PendingContent { get; set; } = new List<ContentModerationItem>();
        public int TotalUsers { get; set; }
        public int PendingApprovals { get; set; }
        public int ActiveEvents { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AdminUserViewModel
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class ContentModerationItem
    {
        public int ItemID { get; set; }
        public string Type { get; set; } = string.Empty; // Service, Review, etc.
        public string Name { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}