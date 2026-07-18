using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("customer_id")]
        public long CustomerId { get; set; }

        [Column("billing_month")]
        public DateOnly BillingMonth { get; set; }

        [Column("payment_date")]
        public DateTime PaymentDate { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("payment_mode")]
        public string PaymentMode { get; set; } = string.Empty;

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        public long? UpdatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByUser { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }
    }
}