using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("attachments")] // DB table stays lowercase — this is correct
    public class Attachments
    {
        [Key]
        [Column("fileID")]
        public int fileID { get; set; }

        [Column("requestID")]
        public int requestID { get; set; }

        [Column("creatorID")]
        public int creatorID { get; set; }

        [MaxLength(256)]
        public string? fileName { get; set; }

        [MaxLength(256)]
        public string? contentType { get; set; }

        [Required, MaxLength(256)]
        public string fileUrl { get; set; } = string.Empty;

        public DateTime uploadedAt { get; set; }

        // Navigation
        [ForeignKey("requestID")]
        public virtual Request? request { get; set; }

        [ForeignKey("creatorID")]
        public virtual User? creator { get; set; }
    }
}