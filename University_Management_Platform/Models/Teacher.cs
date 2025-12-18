using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        [DisplayName("First Name")]
        public string FirstName { get; set; } = null!;
        [Required]
        [StringLength(50)]
        [DisplayName("Last Name")]
        public string LastName { get; set; } = null!;
        
        [StringLength(50)]
        public string? Degree { get; set; }
        
        [StringLength(25)]
        [DisplayName("Academic Rank")]
        public string? AcademicRank { get; set; }
        
        [StringLength(10)]
        [DisplayName("Office Number")]
        public string? OfficeNumber { get; set; }
       
        [DataType(DataType.Date)]
        [DisplayName("Hire Date")]
        public DateTime? HireDate { get; set; }

        public ICollection<Course> FirstCourses { get; set; } = new List<Course>();
        public ICollection<Course> SecondCourses { get; set; } = new List<Course>();

    }
}
