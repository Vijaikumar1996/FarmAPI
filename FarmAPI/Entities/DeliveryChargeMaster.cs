using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("delivery_charge_master")]
    public class DeliveryChargeMaster
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("effective_from")]
        public DateTime EffectiveFrom { get; set; }

        [Column("delivery_charge")]
        public decimal DeliveryCharge { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

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

        [ForeignKey(nameof(CreatedBy))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByUser { get; set; }
    }
}