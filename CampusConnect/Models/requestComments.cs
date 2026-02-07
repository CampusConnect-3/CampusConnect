using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("requestComments")]
    public class requestComments
    {
        [Key]
        [Column("commentID")]
        public int commentID { get; set; }

        [Column("requestID")]
        public int requestID { get; set; }

        [MaxLength(1000)]
        public string? commentText { get; set; }

        public DateTime createdAt { get; set; }

        [Column("creatorID")]
        public int creatorID { get; set; }

        // Navigation
        [ForeignKey("requestID")]
        public virtual request? request { get; set; }

        [ForeignKey("creatorID")]
        public virtual user? creator { get; set; }
    }
}