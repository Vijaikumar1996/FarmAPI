using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("customer_monthly_ledger")]
    public class CustomerMonthlyLedger
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("customer_id")]
        public long CustomerId { get; set; }

        [Column("billing_month")]
        public DateOnly BillingMonth { get; set; }

        [Column("product_amount")]
        public decimal ProductAmount { get; set; }

        [Column("delivery_charge")]
        public decimal DeliveryCharge { get; set; }

        [Column("adjustment_amount")]
        public decimal AdjustmentAmount { get; set; }

        [Column("paid_amount")]
        public decimal PaidAmount { get; set; }

        [Column("balance_amount")]
        public decimal BalanceAmount { get; set; }

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