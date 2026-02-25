using System.ComponentModel.DataAnnotations.Schema;


namespace CampusConnect.Models
{
    [Table("userRoles")]
    public class userRoles
    {
        [Column("roleID")]
        public int roleID { get; set; }

        [Column("userID")]
        public int userID { get; set; }

        // Navigation
        [ForeignKey("userID")]
        public virtual User? user { get; set; }

        // ⭐ MUST reference the EF MODEL, not the enum
        [ForeignKey("roleID")]
        public virtual Roles? role { get; set; }
    }
}