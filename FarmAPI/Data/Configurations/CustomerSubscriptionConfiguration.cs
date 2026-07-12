using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class CustomerSubscriptionConfiguration
    : IEntityTypeConfiguration<CustomerSubscription>
{
    public void Configure(EntityTypeBuilder<CustomerSubscription> builder)
    {
        builder.HasOne(x => x.Customer)
            .WithMany(x => x.CustomerSubscriptions)
            .HasForeignKey(x => x.CustomerId);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.CustomerSubscriptions)
            .HasForeignKey(x => x.ProductId);

        builder.HasOne(x => x.Frequency)
            .WithMany(x => x.CustomerSubscriptions)
            .HasForeignKey(x => x.FrequencyId);

        builder.HasMany(x => x.Schedules)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}