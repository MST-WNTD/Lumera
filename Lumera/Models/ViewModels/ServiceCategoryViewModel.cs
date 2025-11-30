namespace Lumera.Models
{
    public class ServiceCategoryViewModel
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class EditServiceViewModel
    {
        public Organizer Organizer { get; set; } = null!;
        public Service Service { get; set; } = null!;
        public int UnreadMessages { get; set; }
    }
}