using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("subscription_schedules")]
public class SubscriptionSchedule
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("subscription_id")]
    public long SubscriptionId { get; set; }

    [Column("day_of_week")]
    public short? DayOfWeek { get; set; }

    [Column("day_of_month")]
    public short? DayOfMonth { get; set; }

    [Column("pattern_order")]
    public short? PatternOrder { get; set; }

    [Required]
    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(SubscriptionId))]
    public CustomerSubscription Subscription { get; set; } = null!;
}