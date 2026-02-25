using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("userID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int userID { get; set; }

        // Link to AspNetUsers.Id (string)
        [Required, MaxLength(450)]
        public string IdentityUserId { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string fName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string lName { get; set; } = string.Empty;

        // We'll store SchoolId here (unique)
        [Required, MaxLength(256)]
        public string username { get; set; } = string.Empty;

        // Legacy column: do NOT use for auth anymore
        [MaxLength(256)]
        public string? password { get; set; }

        [MaxLength(256)]
        public string? email { get; set; }

        [MaxLength(256)]
        public string? department { get; set; }

        [MaxLength(50)]
        public string? status { get; set; }

        // Navigation
        public virtual ICollection<userRoles> userRoles { get; set; } = new List<userRoles>();
        public virtual ICollection<Request> requestsCreated { get; set; } = new List<Request>();
        public virtual ICollection<Request> requestsAssigned { get; set; } = new List<Request>();
        public virtual ICollection<requestComments> comments { get; set; } = new List<requestComments>();

        // IMPORTANT: this type must match your renamed class Attachments
        public virtual ICollection<Attachments> attachments { get; set; } = new List<Attachments>();
    }
}