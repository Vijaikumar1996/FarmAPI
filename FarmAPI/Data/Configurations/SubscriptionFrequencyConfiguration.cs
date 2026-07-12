using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class SubscriptionFrequencyConfiguration
    : IEntityTypeConfiguration<SubscriptionFrequency>
{
    public void Configure(EntityTypeBuilder<SubscriptionFrequency> builder)
    {
        builder.HasData(

            new SubscriptionFrequency
            {
                Id = 1,
                FrequencyName = "Daily"
            },

            new SubscriptionFrequency
            {
                Id = 2,
                FrequencyName = "Weekly"
            },

            new SubscriptionFrequency
            {
                Id = 3,
                FrequencyName = "Monthly"
            }

        );
    }
}