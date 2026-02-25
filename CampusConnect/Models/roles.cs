using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    // Database table stays lowercase
    [Table("roles")]
    public class Roles
    {
        [Key]
        [Column("roleID")]
        public int roleID { get; set; }

        [Required]
        [MaxLength(256)]
        public string roleName { get; set; } = string.Empty;

        // Navigation
        public virtual ICollection<userRoles> userRoles { get; set; } = new List<userRoles>();
    }
}