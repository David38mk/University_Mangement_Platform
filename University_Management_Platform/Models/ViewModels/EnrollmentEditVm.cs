using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models.ViewModels
{
    public class EnrollmentEditVm
    {
        public long Id { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public long StudentId { get; set; }
        public string StudentIndex { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string? Semester { get; set; }
        public int? Year { get; set; }
        public int? Grade { get; set; }
        public string? SeminalUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public int? ExamPoints { get; set; }
        public int? SeminalPoints { get; set; }
        public int? ProjectPoints { get; set; }
        public int? AdditionalPoints { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FinishDate { get; set; }
    }
}
