using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmAPI.Entities
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("id")]
        public short Id { get; set; }

        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<UserRole> UserRoles
            = new List<UserRole>();
    }
}
