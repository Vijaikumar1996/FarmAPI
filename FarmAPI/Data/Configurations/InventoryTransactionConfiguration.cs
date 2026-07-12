using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Farm.API.Entities.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionDate)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(30);

        builder.Property(x => x.Remarks)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => new
        {
            x.TransactionDate,
            x.ProductId
        });

        builder.HasOne(x => x.Product)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedUser)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}