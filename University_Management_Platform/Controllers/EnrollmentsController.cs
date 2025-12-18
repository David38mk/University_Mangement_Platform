using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using University_Management_Platform.Data;
using University_Management_Platform.Models.ViewModels;

namespace University_Management_Platform.Controllers
{
    public class EnrollmentsController : Controller
    {
        private readonly UniversityDbContext _context;

        public EnrollmentsController(UniversityDbContext context)
        {
            _context = context;
        }

        // GET: Enrollments/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var e = await _context.Enrollments
                .Include(x => x.Course)
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return NotFound();

            var vm = new EnrollmentEditVm
            {
                Id = e.Id,
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                StudentId = e.StudentId,
                StudentIndex = e.Student.StudentId,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,

                Semester = e.Semester,
                Year = e.Year,
                Grade = e.Grade,
                SeminalUrl = e.SeminalUrl,
                ProjectUrl = e.ProjectUrl,
                ExamPoints = e.ExamPoints,
                SeminalPoints = e.SeminalPoints,
                ProjectPoints = e.ProjectPoints,
                AdditionalPoints = e.AdditionalPoints,
                FinishDate = e.FinishDate
            };

            return View(vm);
        }

        // POST: Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, EnrollmentEditVm vm)
        {
            if (id != vm.Id) return NotFound();

            var e = await _context.Enrollments.FindAsync(id);
            if (e == null) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            // FK unchanged!
            e.Semester = vm.Semester;
            e.Year = vm.Year;
            e.Grade = vm.Grade;
            e.SeminalUrl = vm.SeminalUrl;
            e.ProjectUrl = vm.ProjectUrl;
            e.ExamPoints = vm.ExamPoints;
            e.SeminalPoints = vm.SeminalPoints;
            e.ProjectPoints = vm.ProjectPoints;
            e.AdditionalPoints = vm.AdditionalPoints;
            e.FinishDate = vm.FinishDate;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Courses", new { id = e.CourseId });
        }
    }
}
