using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("requestStatus")]
    public class RequestStatus
    {
        [Key]
        [Column("statusID")]
        public int statusID { get; set; }

        [Required, MaxLength(256)]
        public string statusName { get; set; } = string.Empty;

        // Navigation (paired)
        [InverseProperty(nameof(Request.status))]
        public virtual ICollection<Request> requests { get; set; } = new List<Request>();
    }
}