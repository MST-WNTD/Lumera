using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }
        
        [ForeignKey("Client")]
        public int? ClientID { get; set; }
        
        [ForeignKey("Organizer")]
        public int? OrganizerID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string EventName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string EventType { get; set; } = string.Empty;
        
        public string? EventDescription { get; set; }
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Budget { get; set; }
        
        public int? GuestCount { get; set; }
        
        [StringLength(500)]
        public string? Location { get; set; }

        [Required]
        [StringLength(100)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public virtual Client? Client { get; set; }
        public virtual Organizer? Organizer { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}