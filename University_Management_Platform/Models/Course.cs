using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models
{
    public class Course
    {
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = null!;
        [Required]
        [Range(1, 30)]
        public int Credits { get; set; }
        [Required]
        [Range(1, 12)]
        public int Semester { get; set; }
        
        [StringLength(100)]
        public string? Programme { get; set; }
        
        [StringLength(25)]
        public string? EducationLevel { get; set; }
        public int? FirstTeacherId { get; set; }
        public Teacher? FirstTeacher { get; set; }
        public int? SecondTeacherId { get; set; }
        public Teacher? SecondTeacher { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    }
}
