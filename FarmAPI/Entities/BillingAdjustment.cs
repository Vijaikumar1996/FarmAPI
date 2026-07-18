using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("billing_adjustments")]
    public class BillingAdjustment
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("customer_id")]
        public long CustomerId { get; set; }

        [Column("billing_month")]
        public DateOnly BillingMonth { get; set; }

        [Column("adjustment_date")]
        public DateTime AdjustmentDate { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

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