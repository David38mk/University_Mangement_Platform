using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using University_Management_Platform.Models;

namespace University_Management_Platform.Data
{
    public class UniversityDbContext : DbContext
    {
        public UniversityDbContext(DbContextOptions<UniversityDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<EnrollmentSubmission> EnrollmentSubmissions => Set<EnrollmentSubmission>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Course → Teacher (FirstTeacher)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.FirstTeacher)
                .WithMany(t => t.FirstCourses)
                .HasForeignKey(c => c.FirstTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Course → Teacher (SecondTeacher)
            modelBuilder.Entity<Course>()
                .HasOne(c => c.SecondTeacher)
                .WithMany(t => t.SecondCourses)
                .HasForeignKey(c => c.SecondTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enrollment → Course (Course has many Enrollments)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enrollment → Student (Student has many Enrollments)
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate enrollments (same student cannot be enrolled twice in same course)
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.CourseId, e.StudentId })
                .IsUnique();

            //Delete Submissions when Enrollment is deleted
            modelBuilder.Entity<EnrollmentSubmission>()
                .HasOne(es => es.Enrollment)
                .WithMany(e => e.EnrollmentSubmissions)
                .HasForeignKey(es => es.EnrollmentID)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
