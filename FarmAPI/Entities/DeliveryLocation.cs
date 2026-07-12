using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("delivery_locations")]
    public class DeliveryLocation
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("area_id")]
        public long AreaId { get; set; }

        [Column("location_name")]
        public string LocationName { get; set; } = string.Empty;

        [Column("address")]
        public string? Address { get; set; }

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

        [Column("delivery_order")]
        public int DeliveryOrder { get; set; }

        [ForeignKey(nameof(AreaId))]
        public Area? Area { get; set; }
    }
}