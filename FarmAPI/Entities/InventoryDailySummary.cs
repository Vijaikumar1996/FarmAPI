using FarmAPI.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farm.API.Entities;

[Table("inventory_daily_summary")]
public class InventoryDailySummary
{
    public long Id { get; set; }

    public DateOnly StockDate { get; set; }

    public long ProductId { get; set; }

    public decimal OpeningStock { get; set; }

    public decimal AvailableStock { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    #region Navigation Properties

    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedUser { get; set; }

    [ForeignKey(nameof(UpdatedBy))]
    public User? UpdatedUser { get; set; }

    #endregion
}