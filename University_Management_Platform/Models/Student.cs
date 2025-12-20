using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models
{
    public class Student
    {
        public long Id { get; set; }
        [Required]
        [StringLength(10)]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = null!;
        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;
        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;
        
        [DataType(DataType.Date)]
        [Display(Name = "Enrollment Date")] 
        public DateTime? EnrollmentDate { get; set; }
        
        [Display(Name = "Accquired Credits")]
        [Range(0, int.MaxValue, ErrorMessage = "Accquired Credits must be a non-negative number.")]
        public int? AccquiredCredits { get; set; }
        
        [Display(Name = "Current Semester")]
        public int? CurrentSemester { get; set; }
        
        [StringLength(25)]
        [Display(Name = "Education Level")]
        public string? EducationLevel { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public string? photoPath { get; set; }

    }
}
