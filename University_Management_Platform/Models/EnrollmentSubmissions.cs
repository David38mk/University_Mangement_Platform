using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models
{
    public class EnrollmentSubmission
    {
        public int ID { get; set; }
        public long EnrollmentID { get; set; }
        public Enrollment Enrollment { get; set; } = null!;
        [Required]
        [MaxLength(50)]
        public string SubmissionType { get; set; } = null!;
        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = null!;
        [Required]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int Version { get; set; } = 1;
        public bool IsLatest { get; set; } = true;
        [MaxLength(500)]
        public string? Comment { get; set; }
        [MaxLength(64)]
        public string? FileHash { get; set; }
    }
}
