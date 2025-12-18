namespace University_Management_Platform.Models
{
    public class Document
    {
        public int Id { get; set; }

        public long StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int? CourseId { get; set; }      
        public Course? Course { get; set; }

        public string OriginalFileName { get; set; } = "";
        public string StoredFileName { get; set; } = ""; 
        public string ContentType { get; set; } = "";
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

}
