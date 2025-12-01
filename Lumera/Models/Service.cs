using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        [Required]
        public int ProviderID { get; set; }

        [Required]
        public string ProviderType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ServiceName { get; set; } = string.Empty;

        public string? ServiceDescription { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? BasePrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [StringLength(50)]
        public string? PriceType { get; set; }

        [StringLength(500)]
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; } = 0.00m;

        public int TotalReviews { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<ServiceGallery> Gallery { get; set; } = new List<ServiceGallery>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

    public class ServiceGallery
    {
        [Key]
        public int GalleryID { get; set; }

        [ForeignKey("Service")]
        public int ServiceID { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageURL { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Caption { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public virtual Service? Service { get; set; }
    }
}