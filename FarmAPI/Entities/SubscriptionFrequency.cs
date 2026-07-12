using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("subscription_frequencies")]
public class SubscriptionFrequency
{
    [Key]
    [Column("id")]
    public short Id { get; set; }

    [Required]
    [Column("frequency_name")]
    public string FrequencyName { get; set; } = string.Empty;

    public ICollection<CustomerSubscription> CustomerSubscriptions { get; set; }
        = new List<CustomerSubscription>();
}