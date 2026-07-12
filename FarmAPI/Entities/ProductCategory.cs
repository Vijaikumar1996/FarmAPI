using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities;

[Table("product_categories")]
public class ProductCategory
{
    [Key]
    [Column("id")]
    public short Id { get; set; }

    [Column("category_name")]
    [MaxLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Column("track_inventory_default")]
    public bool TrackInventoryDefault { get; set; } = true;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation Property
    public ICollection<Product> Products { get; set; } = new List<Product>();
}