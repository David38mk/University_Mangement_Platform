namespace University_Management_Platform.Models.ViewModels
{
    public class ManageCourseStudentsVm
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public List<StudentPickVm> Students { get; set; } = new();
    }

    public class StudentPickVm
    {
        public long StudentDbId { get; set; }          // Student.Id
        public string StudentIndex { get; set; } = ""; // Student.StudentId
        public string FullName { get; set; } = "";
        public bool IsEnrolled { get; set; }
        public long? EnrollmentId { get; set; }        // Enrollment.Id if enrolled
    }
}
