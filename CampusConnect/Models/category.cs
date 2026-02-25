using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("category")]
    public class Category
    {
        [Key]
        [Column("categoryID")]
        public int categoryID { get; set; }

        [Required, MaxLength(256)]
        public string categoryName { get; set; } = string.Empty;

        // Navigation (paired)
        [InverseProperty(nameof(Request.category))]
        public virtual ICollection<Request> requests { get; set; } = new List<Request>();
    }
}