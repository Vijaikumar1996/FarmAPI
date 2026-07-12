using FarmManagement.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("customers")]
    public class Customer
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("customer_code")]
        public string CustomerCode { get; set; } = string.Empty;

        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [Column("mobile_no")]
        public string MobileNo { get; set; } = string.Empty;

        [Column("alternate_mobile_no")]
        public string? AlternateMobileNo { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("area_id")]
        public long AreaId { get; set; }

        [Column("delivery_location_id")]
        public long? DeliveryLocationId { get; set; }

        [Column("house_door_no")]
        public string HouseDoorNo { get; set; } = string.Empty;

        [Column("landmark")]
        public string? Landmark { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("delivery_notes")]
        public string? DeliveryNotes { get; set; }

        [Column("latitude")]
        public decimal? Latitude { get; set; }

        [Column("longitude")]
        public decimal? Longitude { get; set; }

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

        [Column("user_id")]
        public long? UserId { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(AreaId))]
        public virtual Area? Area { get; set; }

        [ForeignKey(nameof(DeliveryLocationId))]
        public virtual DeliveryLocation? DeliveryLocation { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedBy))]
        public virtual User? UpdatedByUser { get; set; }

        public ICollection<CustomerSubscription> CustomerSubscriptions { get; set; }
        = new List<CustomerSubscription>();

        public virtual ICollection<DeliveryDetail> DeliveryDetails { get; set; } = new List<DeliveryDetail>();
    }
}