using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Organizer
    {
        [Key]
        public int OrganizerID { get; set; }
        
        [ForeignKey("User")]
        public int? UserID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string BusinessName { get; set; } = string.Empty;
        
        public string? BusinessDescription { get; set; }
        
        [StringLength(100)]
        public string? BusinessLicense { get; set; }
        
        public int? YearsOfExperience { get; set; }
        public string? ServiceAreas { get; set; } // JSON stored as string
        public decimal AverageRating { get; set; } = 0.00m;
        public int TotalReviews { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    }
}