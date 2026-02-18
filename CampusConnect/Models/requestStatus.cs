using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("requestStatus")]
    public class requestStatus
    {
        [Key]
        [Column("statusID")]
        public int statusID { get; set; }

        [Required, MaxLength(256)]
        public string statusName { get; set; }

        // Navigation
        public virtual ICollection<request> requests { get; set; } = new List<request>();
    }
}