using System.ComponentModel.DataAnnotations;

namespace BookwormsOnline.Model
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserEmail { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        [MaxLength(500)]
        public string Details { get; set; }

        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public bool IsSuccessful { get; set; }
    }
}
