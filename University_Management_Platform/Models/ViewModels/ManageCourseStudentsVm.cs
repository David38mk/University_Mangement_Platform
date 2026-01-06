using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace University_Management_Platform.Models.ViewModels
{
    public class ManageCourseStudentsVm
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";

        // Batch defaults
        public int DefaultYear { get; set; } = DateTime.Now.Year;
        public string? DefaultSemester { get; set; }

        // LEFT side: available (not enrolled, not pending)
        public List<StudentLookupItemVm> AvailableStudents { get; set; } = new();

        // RIGHT side: pending (to enroll)
        public List<long> PendingStudentIds { get; set; } = new();

        // Bottom table: current enrollments
        public List<CourseEnrollmentRowVm> EnrolledStudents { get; set; } = new();
    }

    public class StudentLookupItemVm
    {
        public long StudentId { get; set; }     // DB Id
        public string FullName { get; set; } = ""; // We'll put "Index - First Last"
        public string? StudentIndex { get; set; }  // Optional (useful for filtering)
        public string? FirstName { get; set; }     // Optional (useful for filtering)
        public string? LastName { get; set; }      // Optional (useful for filtering)
    }

    public class CourseEnrollmentRowVm
    {
        public long EnrollmentId { get; set; }
        public long StudentId { get; set; }
        public string StudentIndex { get; set; } = "";
        public string FullName { get; set; } = "";
        public int? Year { get; set; }
        public string? Semester { get; set; }
        public DateTime? FinishDate { get; set; }

        public bool IsActive => FinishDate == null;
    }

    public class EndEnrollmentVm
    {
        public int CourseId { get; set; }
        public long EnrollmentId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime FinishDate { get; set; }
    }
}
