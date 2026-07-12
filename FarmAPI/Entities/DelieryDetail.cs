using FarmAPI.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmManagement.Entities;

[Table("delivery_details")]
public class DeliveryDetail
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("delivery_date")]
    public DateOnly DeliveryDate { get; set; }

    [Required]
    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Required]
    [Column("product_id")]
    public long ProductId { get; set; }

    [Required]
    [Column("planned_qty")]
    public decimal PlannedQty { get; set; }

    [Required]
    [Column("delivered_qty")]
    public decimal DeliveredQty { get; set; }

    [Required]
    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = "DELIVERED";

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("subscription_id")]
    public long? SubscriptionId { get; set; }

    [Column("request_id")]
    public long? RequestId { get; set; }

    [Required]
    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [Column("generated_by")]
    public long? GeneratedBy { get; set; }

    #region Navigation Properties

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey(nameof(SubscriptionId))]
    public virtual CustomerSubscription? Subscription { get; set; }

    [ForeignKey(nameof(RequestId))]
    public virtual CustomerRequest? Request { get; set; }

    [ForeignKey(nameof(GeneratedBy))]
    public virtual User? GeneratedByUser { get; set; }

    #endregion
}