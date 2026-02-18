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
        public virtual user? user { get; set; }

        [ForeignKey("roleID")]
        public virtual roles? role { get; set; }
    }
}