using Farm.API.Entities;
using FarmManagement.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("username")]
        public string UserName { get; set; } = string.Empty;

        [Column("mobile_no")]
        public string MobileNo { get; set; } = string.Empty;

        [Column("email")]
        public string? Email { get; set; }

        [Column("password_hash")]
        public string? PasswordHash { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<UserRole> UserRoles
        = new List<UserRole>();

        public ICollection<InventoryDailySummary> CreatedInventoryDailySummaries { get; set; }
    = new List<InventoryDailySummary>();

        public ICollection<InventoryDailySummary> UpdatedInventoryDailySummaries { get; set; }
            = new List<InventoryDailySummary>();

        public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
            = new List<InventoryTransaction>();

        public virtual ICollection<DeliveryDetail> GeneratedDeliveryDetails { get; set; } = new List<DeliveryDetail>();
    }
}
