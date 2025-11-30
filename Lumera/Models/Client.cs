using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Client
    {
        [Key]
        public int ClientID { get; set; }
        
        [ForeignKey("User")]
        public int? UserID { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        public string? PreferredEventTypes { get; set; } // JSON stored as string
        public bool NewsletterSubscription { get; set; } = true;
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}