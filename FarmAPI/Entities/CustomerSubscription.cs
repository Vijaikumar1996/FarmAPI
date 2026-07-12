using FarmManagement.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("customer_subscriptions")]
public class CustomerSubscription
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Required]
    [Column("product_id")]
    public long ProductId { get; set; }    

    [Required]
    [Column("frequency_id")]
    public short FrequencyId { get; set; }

    [Required]
    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("interval_days")]
    public short? IntervalDays { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation Properties

    [ForeignKey(nameof(CustomerId))]
    public Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(FrequencyId))]
    public SubscriptionFrequency Frequency { get; set; } = null!;

    public ICollection<SubscriptionSchedule> Schedules { get; set; }
        = new List<SubscriptionSchedule>();

    public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; } = new List<DeliveryDetail>();
}