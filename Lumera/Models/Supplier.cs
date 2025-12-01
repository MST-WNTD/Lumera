using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierID { get; set; }
        
        [ForeignKey("User")]
        public int? UserID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string BusinessName { get; set; } = string.Empty;
        
        public string? BusinessDescription { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ServiceCategory { get; set; } = string.Empty;
        
        public string? ServiceAreas { get; set; } // JSON stored as string
        public int? YearsOfExperience { get; set; }
        public decimal AverageRating { get; set; } = 0.00m;
        public int TotalReviews { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<Service> Services { get; set; } = new List<Service>();
    }
}