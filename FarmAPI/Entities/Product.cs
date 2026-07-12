using Farm.API.Entities;
using FarmManagement.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("products")]
public class Product
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("product_code")]
    [MaxLength(20)]
    public string ProductCode { get; set; } = string.Empty;

    [Column("product_name")]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Column("category_id")]
    public short CategoryId { get; set; }

    [Column("litres_per_unit")]
    public decimal? LitresPerUnit { get; set; }

    [Column("track_inventory")]
    public bool TrackInventory { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    // Navigation Properties
    public ProductCategory Category { get; set; } = null!;

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public ICollection<ProductPrice> ProductPrices { get; set; } = new List<ProductPrice>();

    public ICollection<CustomerSubscription> CustomerSubscriptions { get; set; }
    = new List<CustomerSubscription>();

    public ICollection<InventoryDailySummary> InventoryDailySummaries { get; set; }
    = new List<InventoryDailySummary>();

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
        = new List<InventoryTransaction>();

    public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; } = new List<DeliveryDetail>();
}