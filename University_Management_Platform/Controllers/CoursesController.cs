using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using University_Management_Platform.Areas.Identity.Data;
using University_Management_Platform.Data;
using University_Management_Platform.Models;
using University_Management_Platform.Models.ViewModels;

namespace University_Management_Platform.Controllers
{
    public class CoursesController : Controller
    {
        private readonly UniversityDbContext _context;
        private readonly UserManager<University_Management_PlatformUser> _userManager;


        public CoursesController(UniversityDbContext context, UserManager<University_Management_PlatformUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // GET: Courses
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index(string? title, int? semester, string? programme)
        {
            var q = _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                q = q.Where(c => c.Title.Contains(title));

            if (semester.HasValue)
                q = q.Where(c => c.Semester == semester.Value);

            if (!string.IsNullOrWhiteSpace(programme))
                q = q.Where(c => c.Programme != null && c.Programme.Contains(programme));

            ViewBag.Title = title;
            ViewBag.Semester = semester;
            ViewBag.Programme = programme;

            return View(await q.ToListAsync());
        }

        //Teacher only access
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            var courses = await _context.Courses
                .Where(c => c.FirstTeacherId == teacher.Id || c.SecondTeacherId == teacher.Id)
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .OrderBy(c => c.Semester)
                .ThenBy(c => c.Title)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View("MyCourses", courses);
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyCourseStudents(int id, int? year)
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            // security: teacher must teach this course
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && (c.FirstTeacherId == teacher.Id || c.SecondTeacherId == teacher.Id));

            if (course == null) return Forbid();

            // choose default year = latest year in enrollments for this course
            int selectedYear;
            if (year.HasValue)
                selectedYear = year.Value;
            else
                selectedYear = await _context.Enrollments
                    .Where(e => e.CourseId == id && e.Year.HasValue)
                    .MaxAsync(e => (int?)e.Year) ?? DateTime.Now.Year;

            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == id && e.Year == selectedYear)
                .Include(e => e.Student)
                .OrderBy(e => e.Student.StudentId)
                .ToListAsync();

            // years for dropdown
            var years = await _context.Enrollments
                .Where(e => e.CourseId == id && e.Year.HasValue)
                .Select(e => e.Year!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            ViewBag.Course = course;
            ViewBag.Years = years;
            ViewBag.SelectedYear = selectedYear;

            return View("MyCourseStudents", enrollments);
        }


        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> TeacherEditEnrollment(long id)
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            var e = await _context.Enrollments
                .Include(x => x.Course)
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return NotFound();

            // security: must be teacher’s course
            if (!(e.Course.FirstTeacherId == teacher.Id || e.Course.SecondTeacherId == teacher.Id))
                return Forbid();

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
                ExamPoints = e.ExamPoints,
                SeminalPoints = e.SeminalPoints,
                ProjectPoints = e.ProjectPoints,
                AdditionalPoints = e.AdditionalPoints,
                FinishDate = e.FinishDate
            };

            return View("TeacherEditEnrollment", vm);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TeacherEditEnrollment(EnrollmentEditVm vm)
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            var e = await _context.Enrollments
                .Include(x => x.Course)
                .FirstOrDefaultAsync(x => x.Id == vm.Id);

            if (e == null) return NotFound();

            if (!(e.Course.FirstTeacherId == teacher.Id || e.Course.SecondTeacherId == teacher.Id))
                return Forbid();

            // only for active students (spec rule)
            if (e.FinishDate != null)
            {
                TempData["Error"] = "This enrollment is finished. You cannot modify points/grade.";
                return RedirectToAction(nameof(MyCourseStudents), new { id = e.CourseId, year = e.Year });
            }

            e.ExamPoints = vm.ExamPoints;
            e.SeminalPoints = vm.SeminalPoints;
            e.ProjectPoints = vm.ProjectPoints;
            e.AdditionalPoints = vm.AdditionalPoints;
            e.Grade = vm.Grade;
            e.FinishDate = vm.FinishDate;

            await _context.SaveChangesAsync();

            TempData["Ok"] = "Enrollment updated.";
            return RedirectToAction(nameof(MyCourseStudents), new { id = e.CourseId, year = e.Year });
        }


        // GET: Courses/StudentMyCourses
        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> StudentMyCourses()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (student == null) return NotFound();

            // All enrollments for the logged-in student (active + finished)
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == student.Id)
                .Include(e => e.Course)
                    .ThenInclude(c => c.FirstTeacher)
                .Include(e => e.Course)
                    .ThenInclude(c => c.SecondTeacher)
                .OrderByDescending(e => e.Year)
                .ThenBy(e => e.Course.Title)
                .ToListAsync();

            return View(enrollments);
        }



        // GET: Courses/ByTeacher?teacherId=5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ByTeacher(int teacherId)
        {
            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null) return NotFound();

            var courses = await _context.Courses
                .Where(c => c.FirstTeacherId == teacherId || c.SecondTeacherId == teacherId)
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .ToListAsync();

            ViewBag.Teacher = teacher;
            return View(courses);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageStudents(int id)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            // All enrollments for bottom table (active + finished)
            var enrolled = await _context.Enrollments
                .Where(e => e.CourseId == id)
                .Include(e => e.Student)
                .OrderBy(e => e.Student.StudentId)
                .ToListAsync();

            // Active enrolled student IDs (should NOT appear in available/pending)
            var activeEnrolledIds = enrolled
                .Where(e => e.FinishDate == null)
                .Select(e => e.StudentId)
                .ToHashSet();

            // Available students = NOT actively enrolled
            var availableStudents = await _context.Students
                .Where(s => !activeEnrolledIds.Contains(s.Id))
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            var vm = new ManageCourseStudentsVm
            {
                CourseId = course.Id,
                CourseTitle = course.Title,

                AvailableStudents = availableStudents.Select(s => new StudentLookupItemVm
                {
                    StudentId = s.Id,
                    StudentIndex = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = $"{s.StudentId} - {s.FirstName} {s.LastName}"
                }).ToList(),

                EnrolledStudents = enrolled.Select(e => new CourseEnrollmentRowVm
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    StudentIndex = e.Student.StudentId,
                    FullName = e.Student.FirstName + " " + e.Student.LastName,
                    Year = e.Year,
                    Semester = e.Semester,
                    FinishDate = e.FinishDate
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudentsBatch(ManageCourseStudentsVm vm)
        {
            if (vm.PendingStudentIds == null || vm.PendingStudentIds.Count == 0)
            {
                TempData["Error"] = "No students were added to the batch.";
                return RedirectToAction(nameof(ManageStudents), new { id = vm.CourseId });
            }

            // Distinct to avoid duplicates from client
            var ids = vm.PendingStudentIds.Distinct().ToList();

            // Pull all existing enrollments for these students in this course
            var existing = await _context.Enrollments
                .Where(e => e.CourseId == vm.CourseId && ids.Contains(e.StudentId))
                .ToListAsync();

            var existingMap = existing.ToDictionary(e => e.StudentId, e => e);

            int added = 0;
            int reactivated = 0;
            int alreadyActive = 0;

            foreach (var studentId in ids)
            {
                if (existingMap.TryGetValue(studentId, out var enr))
                {
                    if (enr.FinishDate != null)
                    {
                        enr.FinishDate = null;
                        enr.Year = vm.DefaultYear;
                        enr.Semester = vm.DefaultSemester;
                        reactivated++;
                    }
                    else
                    {
                        alreadyActive++;
                    }
                }
                else
                {
                    _context.Enrollments.Add(new Enrollment
                    {
                        CourseId = vm.CourseId,
                        StudentId = studentId,
                        Year = vm.DefaultYear,
                        Semester = vm.DefaultSemester
                    });
                    added++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Ok"] = $"Batch complete: Added {added}, re-activated {reactivated}, already active {alreadyActive}.";
            return RedirectToAction(nameof(ManageStudents), new { id = vm.CourseId });
        }


        // ✅ POST: end enrollment with finish date (soft remove)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndEnrollment(EndEnrollmentVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please select a valid finish date.";
                return RedirectToAction(nameof(ManageStudents), new { id = vm.CourseId });
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == vm.EnrollmentId && e.CourseId == vm.CourseId);

            if (enrollment == null) return NotFound();

            enrollment.FinishDate = vm.FinishDate;
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Enrollment ended (FinishDate set).";
            return RedirectToAction(nameof(ManageStudents), new { id = vm.CourseId });
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Courses/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] Course course)
        {
            if (ModelState.IsValid)
            {
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.SecondTeacherId);
            return View(course);
        }

        // GET: Courses/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);
            return View(course);
        }

        // POST: Courses/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] Course course)
        {
            if (id != course.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FullName", course.SecondTeacherId);
            return View(course);
        }

        // GET: Courses/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // POST: Courses/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
