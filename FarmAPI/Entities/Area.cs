using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("areas")]
    public class Area
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("area_code")]
        public string AreaCode { get; set; } = string.Empty;

        [Column("area_name")]
        public string AreaName { get; set; } = string.Empty;

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("group_delivery_sheet_by_location")]
        public bool GroupDeliverySheetByLocation { get; set; }

        // Controls whether a blank space is added
        // after each delivery location for this area
        [Column("show_space_after_delivery_location")]
        public bool ShowSpaceAfterLocation { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        public long? UpdatedBy { get; set; }
    }
}