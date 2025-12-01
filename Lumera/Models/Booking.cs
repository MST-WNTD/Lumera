using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }
        
        [ForeignKey("Event")]
        public int? EventID { get; set; }
        
        [ForeignKey("Service")]
        public int? ServiceID { get; set; }
        
        [ForeignKey("Client")]
        public int? ClientID { get; set; }
        
        [Required]
        public int ProviderID { get; set; }
        
        [Required]
        public string ProviderType { get; set; } = string.Empty;
        
        public DateTime BookingDate { get; set; } = DateTime.Now;
        
        [Required]
        public DateTime EventDate { get; set; }
        
        public string? ServiceDetails { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? QuoteAmount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? FinalAmount { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending";
        
        public string? ClientNotes { get; set; }
        public string? ProviderNotes { get; set; }
        
        // Navigation properties
        public virtual Event? Event { get; set; }
        public virtual Service? Service { get; set; }
        public virtual Client? Client { get; set; }
        public virtual ICollection<BookingMessage> Messages { get; set; } = new List<BookingMessage>();
    }
    
    public class BookingMessage
    {
        [Key]
        public int MessageID { get; set; }
        
        [ForeignKey("Booking")]
        public int BookingID { get; set; }
        
        [ForeignKey("Sender")]
        public int SenderID { get; set; }
        
        [Required]
        public string MessageText { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? AttachmentURL { get; set; }
        
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.Now;
        
        public virtual Booking? Booking { get; set; }
        public virtual User? Sender { get; set; }
    }
}