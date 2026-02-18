using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("category")]
    public class category
    {
        [Key]
        [Column("categoryID")]
        public int categoryID { get; set; }

        [Required, MaxLength(256)]
        public string categoryName { get; set; }

        // Navigation
        public virtual ICollection<request> requests { get; set; } = new List<request>();
    }
}