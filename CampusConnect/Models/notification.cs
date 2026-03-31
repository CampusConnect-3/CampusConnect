using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusConnect.Models
{
    [Table("notifications")]
    public class notification
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Required, MaxLength(450)]
        [Column("userId")]
        public string userId { get; set; } = string.Empty;

        [Column("requestId")]
        public int requestId { get; set; }

        [Required, MaxLength(200)]
        public string title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string message { get; set; } = string.Empty;

        public bool isRead { get; set; }

        public DateTimeOffset createdAt { get; set; }

        [MaxLength(200)]
        public string? assigneeName { get; set; }

        [MaxLength(200)]
        public string? assigneeRole { get; set; }

        [MaxLength(100)]
        public string? primaryActionText { get; set; }

        [MaxLength(2048)]
        public string? primaryActionUrl { get; set; }

        [ForeignKey("requestId")]
        public virtual request? request { get; set; }
    }
}
