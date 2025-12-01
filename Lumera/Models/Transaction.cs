using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lumera.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionID { get; set; }

        [ForeignKey("Booking")]
        public int? BookingID { get; set; }

        [ForeignKey("Payer")]
        public int PayerID { get; set; }

        [ForeignKey("Payee")]
        public int PayeeID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string TransactionType { get; set; } = "Booking"; // Booking, Deposit, Final Payment, Refund

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? StripePaymentIntentID { get; set; }

        // Navigation properties
        public virtual Booking? Booking { get; set; }
        public virtual User? Payer { get; set; }
        public virtual User? Payee { get; set; }

        // ✅ REMOVED: These lines were causing the error
        // public virtual Client? Client => Booking?.Client;
        // [ForeignKey("Client")]
        // public int? ClientID { get; set; }

        // ✅ ADD: Computed property to access Client through Booking (read-only)
        [NotMapped]
        public Client? Client => Booking?.Client;

        // ✅ ADD: Helper property for Description
        [NotMapped]
        public string Description => $"{TransactionType} - {Booking?.Service?.ServiceName ?? "Service"}";
    }

    public class Payout
    {
        [Key]
        public int PayoutID { get; set; }

        [ForeignKey("Payee")]
        public int PayeeID { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? PayoutMethod { get; set; }

        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User? Payee { get; set; }
    }

    public class PayoutRequest
    {
        public decimal Amount { get; set; }
        public string? PayoutMethod { get; set; }
    }
}