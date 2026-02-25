using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("request")]
    public class Request
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
        public string title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string description { get; set; } = string.Empty;

        [Column("categoryID")]
        public int categoryID { get; set; }

        [Required, MaxLength(10)]
        public string priority { get; set; } = string.Empty;

        [Column("statusID")]
        public int? statusID { get; set; }

        public DateTime createdAt { get; set; }

        public DateTime? closedAt { get; set; }

        [Required, MaxLength(256)]
        public string buildingName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string roomNumber { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? phoneNumber { get; set; }

        [Required, MaxLength(256)]
        public string email { get; set; } = string.Empty;

        // -----------------------------
        // Navigation (explicit + paired)
        // -----------------------------

        [ForeignKey(nameof(created_by))]
        public virtual User? createdBy { get; set; }

        [ForeignKey(nameof(assigned_to))]
        public virtual User? assignedTo { get; set; }

        [ForeignKey(nameof(categoryID))]
        [InverseProperty(nameof(Category.requests))]
        public virtual Category? category { get; set; }

        [ForeignKey(nameof(statusID))]
        [InverseProperty(nameof(RequestStatus.requests))]
        public virtual RequestStatus? status { get; set; }

        public virtual ICollection<requestComments> comments { get; set; } = new List<requestComments>();
        public virtual ICollection<Attachments> attachments { get; set; } = new List<Attachments>();
    }
}