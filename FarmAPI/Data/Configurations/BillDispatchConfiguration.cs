using FarmAPI.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FarmAPI.Data.Configurations
{
    public class BillDispatchConfiguration
        : IEntityTypeConfiguration<BillDispatch>
    {
        public void Configure(EntityTypeBuilder<BillDispatch> builder)
        {
            builder.ToTable("bill_dispatches");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CustomerId)
                .HasColumnName("customer_id");

            builder.Property(x => x.BillingMonth)
                .HasColumnName("billing_month");

            builder.Property(x => x.TotalAmount)
                .HasColumnName("total_amount")
                .HasPrecision(12, 2);

            builder.Property(x => x.BillStatus)
                .HasColumnName("bill_status")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.SentAt)
                .HasColumnName("sent_at");

            builder.Property(x => x.SentBy)
                .HasColumnName("sent_by");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            builder.HasIndex(x => new
            {
                x.CustomerId,
                x.BillingMonth
            })
            .IsUnique();
        }
    }
}