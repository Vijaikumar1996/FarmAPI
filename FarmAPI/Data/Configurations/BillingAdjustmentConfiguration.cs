using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class BillingAdjustmentConfiguration
    : IEntityTypeConfiguration<BillingAdjustment>
{
    public void Configure(EntityTypeBuilder<BillingAdjustment> builder)
    {
        builder
            .HasOne(x => x.Customer)
            .WithMany(x => x.BillingAdjustments)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new
            {
                x.CustomerId,
                x.BillingMonth
            });

        builder
            .Property(x => x.Amount)
            .HasPrecision(12, 2);

        builder
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.UpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}