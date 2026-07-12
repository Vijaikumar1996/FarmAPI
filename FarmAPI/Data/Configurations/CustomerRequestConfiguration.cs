using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Configurations;

public class CustomerRequestConfiguration : IEntityTypeConfiguration<CustomerRequest>
{
    public void Configure(EntityTypeBuilder<CustomerRequest> builder)
    {
        builder.ToTable("customer_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestAction)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(10, 2);

        builder.Property(x => x.Remarks);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.CustomerId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.EffectiveFrom
        });
    }
}