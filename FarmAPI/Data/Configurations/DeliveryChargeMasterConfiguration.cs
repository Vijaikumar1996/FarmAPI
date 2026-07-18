using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations;

public class DeliveryChargeMasterConfiguration
    : IEntityTypeConfiguration<DeliveryChargeMaster>
{
    public void Configure(EntityTypeBuilder<DeliveryChargeMaster> builder)
    {
        builder
            .Property(x => x.DeliveryCharge)
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