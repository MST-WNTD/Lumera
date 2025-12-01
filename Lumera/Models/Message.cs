using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Conversation
    {
        [Key]
        public int ConversationID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastMessageAt { get; set; } = DateTime.Now;

        // CHANGED: Make EventID nullable and remove required constraint
        [ForeignKey("Event")]
        public int? EventID { get; set; }

        // NEW: Direct reference to Client for persistence after event deletion
        [ForeignKey("Client")]
        public int? ClientID { get; set; }

        // NEW: Direct reference to Organizer for persistence
        [ForeignKey("Organizer")]
        public int? OrganizerID { get; set; }

        [Required]
        public string ConversationType { get; set; } = "Direct"; // Direct or Group

        // Navigation properties
        public virtual Event? Event { get; set; }
        public virtual Client? Client { get; set; }
        public virtual Organizer? Organizer { get; set; }
        public virtual ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class ConversationParticipant
    {
        [Key]
        public int ParticipantID { get; set; }

        [ForeignKey("Conversation")]
        public int ConversationID { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public DateTime? LeftAt { get; set; }

        // Navigation properties
        public virtual Conversation? Conversation { get; set; }
        public virtual User? User { get; set; }
    }

    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [ForeignKey("Conversation")]
        public int ConversationID { get; set; }

        [ForeignKey("Sender")]
        public int SenderID { get; set; }

        [Required]
        public string MessageText { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AttachmentURL { get; set; }

        [Required]
        public string MessageType { get; set; } = "Text"; // Text, Image, File, System

        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Conversation? Conversation { get; set; }
        public virtual User? Sender { get; set; }
    }
}
