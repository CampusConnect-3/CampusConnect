using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("attachments")]
    public class attachments
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
        public string fileUrl { get; set; }

        public DateTime uploadedAt { get; set; }

        // Navigation
        [ForeignKey("requestID")]
        public virtual request? request { get; set; }

        [ForeignKey("creatorID")]
        public virtual user? creator { get; set; }
    }
}