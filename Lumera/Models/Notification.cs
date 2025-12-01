using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        public int? ReferenceID { get; set; }

        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        [MaxLength(500)]
        public string? RedirectUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
    }
}