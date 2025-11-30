using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [ForeignKey("Booking")]
        public int? BookingID { get; set; }

        [ForeignKey("Reviewer")]
        public int? ReviewerID { get; set; }

        [Required]
        public int RevieweeID { get; set; }

        [Required]
        public string RevieweeType { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? ReviewText { get; set; }
        public bool IsApproved { get; set; } = true;
        public bool IsEdited { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Booking? Booking { get; set; }
        public virtual User? Reviewer { get; set; }
    }
}