using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("request")]
    public class request
    {
        [Key]
        [Column("requestID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int requestID { get; set; }

        [Column("created_by")]
        public int created_by { get; set; }

        [Column("assigned_to")]
        public int? assigned_to { get; set; }

        [Required, MaxLength(256)]
        public string title { get; set; }

        [Required, MaxLength(1000)]
        public string description { get; set; }

        [Column("categoryID")]
        public int categoryID { get; set; }

        [Required, MaxLength(10)]
        public string priority { get; set; }

        [Column("statusID")]
        public int? statusID { get; set; }

        public DateTime createdAt { get; set; }

        public DateTime? closedAt { get; set; }  // Changed to nullable

        [Required, MaxLength(256)]
        public string buildingName { get; set; }

        [Required, MaxLength(256)]
        public string roomNumber { get; set; }

        [MaxLength(256)]
        public string? phoneNumber { get; set; }

        [Required, MaxLength(256)]
        public string email { get; set; }

        // Navigation
        [ForeignKey("created_by")]
        public virtual user? createdBy { get; set; }

        [ForeignKey("assigned_to")]
        public virtual user? assignedTo { get; set; }

        [ForeignKey("categoryID")]
        public virtual category? category { get; set; }

        [ForeignKey("statusID")]
        public virtual requestStatus? status { get; set; }

        public virtual ICollection<requestComments> comments { get; set; } = new List<requestComments>();
        public virtual ICollection<attachments> attachments { get; set; } = new List<attachments>();
    }
}