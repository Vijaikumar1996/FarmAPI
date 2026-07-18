using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class CustomerOutstandingConfiguration
    : IEntityTypeConfiguration<CustomerOutstanding>
{
    public void Configure(EntityTypeBuilder<CustomerOutstanding> builder)
    {
        builder
            .HasOne(x => x.Customer)
            .WithOne(x => x.CustomerOutstanding)
            .HasForeignKey<CustomerOutstanding>(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(x => x.OutstandingAmount)
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