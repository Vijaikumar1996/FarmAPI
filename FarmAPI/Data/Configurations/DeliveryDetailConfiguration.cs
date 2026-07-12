using FarmManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Configurations;

public class DeliveryDetailConfiguration : IEntityTypeConfiguration<DeliveryDetail>
{
    public void Configure(EntityTypeBuilder<DeliveryDetail> builder)
    {
        builder.ToTable("delivery_details");

        // Primary Key
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.PlannedQty)
            .HasPrecision(10, 2);

        builder.Property(x => x.DeliveredQty)
            .HasPrecision(10, 2);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Remarks);

        builder.Property(x => x.GeneratedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.DeliveryDetails)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.DeliveryDetails)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.DeliveryDetails)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Request)
            .WithMany(x => x.DeliveryDetails)
            .HasForeignKey(x => x.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GeneratedByUser)
            .WithMany(x => x.GeneratedDeliveryDetails)
            .HasForeignKey(x => x.GeneratedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes

        // Used while deleting/regenerating deliveries for a date
        builder.HasIndex(x => x.DeliveryDate);

        // Delivery planning (Customer wise)
        builder.HasIndex(x => new
        {
            x.DeliveryDate,
            x.CustomerId
        });

        // Farm summary / Product summary
        builder.HasIndex(x => new
        {
            x.DeliveryDate,
            x.ProductId
        });

        // Delivery status reports
        builder.HasIndex(x => new
        {
            x.DeliveryDate,
            x.Status
        });

        // Customer delivery history
        builder.HasIndex(x => new
        {
            x.CustomerId,
            x.DeliveryDate
        });

        // Product delivery history
        builder.HasIndex(x => new
        {
            x.ProductId,
            x.DeliveryDate
        });

        // Foreign key indexes
        builder.HasIndex(x => x.SubscriptionId);

        builder.HasIndex(x => x.RequestId);

        builder.HasIndex(x => x.GeneratedBy);
    }
}