using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class CustomerMonthlyLedgerConfiguration
    : IEntityTypeConfiguration<CustomerMonthlyLedger>
{
    public void Configure(EntityTypeBuilder<CustomerMonthlyLedger> builder)
    {
        builder
            .HasOne(x => x.Customer)
            .WithMany(x => x.CustomerMonthlyLedgers)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new
            {
                x.CustomerId,
                x.BillingMonth
            })
            .IsUnique();

        builder
            .Property(x => x.ProductAmount)
            .HasPrecision(12, 2);

        builder
            .Property(x => x.DeliveryCharge)
            .HasPrecision(12, 2);

        builder
            .Property(x => x.AdjustmentAmount)
            .HasPrecision(12, 2);

        builder
            .Property(x => x.PaidAmount)
            .HasPrecision(12, 2);

        builder
            .Property(x => x.BalanceAmount)
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