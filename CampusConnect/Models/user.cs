using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("users")]
    public class user
    {
        [Key]
        [Column("userID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int userID { get; set; }

        [Required, MaxLength(256)]
        public string fName { get; set; }

        [Required, MaxLength(256)]
        public string lName { get; set; }

        [Required, MaxLength(256)]
        public string username { get; set; }

        // Remove [Required] and make nullable (or delete this property if you will drop the column)
        [MaxLength(256)]
        public string? password { get; set; }

        [Required, MaxLength(256)]
        public string email { get; set; }

        [MaxLength(256)]
        public string? department { get; set; }

        [MaxLength(50)]
        public string? status { get; set; }

        // Link to AspNetUsers.Id (Identity user)
        [MaxLength(450)]
        public string? identityUserId { get; set; }

        // Navigation
        public virtual ICollection<userRoles> userRoles { get; set; } = new List<userRoles>();
        public virtual ICollection<request> requestsCreated { get; set; } = new List<request>();
        public virtual ICollection<request> requestsAssigned { get; set; } = new List<request>();
        public virtual ICollection<requestComments> comments { get; set; } = new List<requestComments>();
        public virtual ICollection<attachments> attachments { get; set; } = new List<attachments>();
    }
}