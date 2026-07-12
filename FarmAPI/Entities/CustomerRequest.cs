using FarmManagement.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("customer_requests")]
public class CustomerRequest
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Required]
    [Column("request_action")]
    public string RequestAction { get; set; } = string.Empty;

    [Column("product_id")]
    public long? ProductId { get; set; }

    [Column("quantity")]
    public decimal? Quantity { get; set; }

    [Required]
    [Column("effective_from")]
    public DateOnly EffectiveFrom { get; set; }

    [Column("effective_to")]
    public DateOnly? EffectiveTo { get; set; }

    [Column("remarks")]
    public string? Remarks { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("subscription_id")]
    public long? SubscriptionId { get; set; }

    // Navigation Properties

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; } = new List<DeliveryDetail>();
}