using System.ComponentModel.DataAnnotations;

namespace Lumera.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required]
        public string Role { get; set; } = "Client"; // Client, Organizer, Supplier, Admin
        
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(500)]
        public string? AvatarURL { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
        
        // Navigation properties
        public virtual Client? Client { get; set; }
        public virtual Organizer? Organizer { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}