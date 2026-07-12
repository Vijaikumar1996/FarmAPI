using Farm.API.Enums;
using FarmAPI.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Farm.API.Entities;

[Table("inventory_transactions")]
public class InventoryTransaction
{
    public long Id { get; set; }

    public DateOnly TransactionDate { get; set; }

    public long ProductId { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    public decimal Quantity { get; set; }

    public string? Remarks { get; set; }

    public long? ReferenceId { get; set; }

    public string? ReferenceType { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    #region Navigation Properties

    public Product Product { get; set; } = null!;

    [ForeignKey(nameof(CreatedBy))]
    public User? CreatedUser { get; set; }

    #endregion
}