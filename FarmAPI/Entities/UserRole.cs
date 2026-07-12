using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("user_roles")]
    public class UserRole
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("role_id")]
        public short RoleId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;

        public Role Role { get; set; } = null!;
    }
}
