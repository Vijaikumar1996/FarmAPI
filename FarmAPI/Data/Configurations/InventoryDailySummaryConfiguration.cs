using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Farm.API.Entities.Configurations;

public class InventoryDailySummaryConfiguration : IEntityTypeConfiguration<InventoryDailySummary>
{
    public void Configure(EntityTypeBuilder<InventoryDailySummary> builder)
    {
        builder.ToTable("inventory_daily_summary");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StockDate)
            .IsRequired();

        builder.Property(x => x.OpeningStock)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.AvailableStock)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Remarks)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new
        {
            x.StockDate,
            x.ProductId
        }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryDailySummaries)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedUser)
            .WithMany(x => x.CreatedInventoryDailySummaries)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UpdatedUser)
            .WithMany(x => x.UpdatedInventoryDailySummaries)
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}