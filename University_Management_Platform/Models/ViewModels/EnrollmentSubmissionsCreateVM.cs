using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.ViewModels.EnrollmentSubmissions
{
    public class EnrollmentSubmissionsCreateVM
    {
        [Required]
        public int EnrollmentID { get; set; }

        [Required]
        [MaxLength(50)]
        public string SubmissionType { get; set; } = null!;

        [Required]
        public IFormFile File { get; set; } = null!;

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
