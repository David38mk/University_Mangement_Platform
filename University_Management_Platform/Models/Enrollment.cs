using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models
{
    public class Enrollment
    {
        public long Id { get; set; }
        
        public int CourseId { get; set; }
        [Required]
        public Course Course { get; set; } = null!;
        
        public long StudentId { get; set; }
        [Required]
        public Student Student { get; set; } = null!;
        [StringLength(10)]
        public string? Semester { get; set; }
        
        public int? Year { get; set; }
        public int? Grade { get; set; }
        [StringLength(255)]
        public string? SeminalUrl { get; set; }
        [StringLength(255)]
        public string? ProjectUrl { get; set; }
        public int? ExamPoints { get; set; }
        public int? SeminalPoints { get; set; }
        public int? ProjectPoints { get; set; }
        public int? AdditionalPoints { get; set; }
        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }
        
        public ICollection<EnrollmentSubmission> EnrollmentSubmissions { get; set; } = new List<EnrollmentSubmission>();

    }
}
