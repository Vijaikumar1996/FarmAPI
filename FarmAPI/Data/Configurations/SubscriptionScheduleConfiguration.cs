using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class SubscriptionScheduleConfiguration
    : IEntityTypeConfiguration<SubscriptionSchedule>
{
    public void Configure(EntityTypeBuilder<SubscriptionSchedule> builder)
    {
        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}