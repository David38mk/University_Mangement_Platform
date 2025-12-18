using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using University_Management_Platform.Data;
using University_Management_Platform.Models;
using University_Management_Platform.Models.ViewModels;

namespace University_Management_Platform.Controllers
{
    public class CoursesController : Controller
    {
        private readonly UniversityDbContext _context;

        public CoursesController(UniversityDbContext context)
        {
            _context = context;
        }

        // GET: Courses
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


        // GET: Courses/ByTeacher?teacherId=5  (Courses by teacher)
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

        public async Task<IActionResult> ManageStudents(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var enrollmentMap = course.Enrollments.ToDictionary(e => e.StudentId, e => e);

            var students = await _context.Students.ToListAsync();

            var vm = new ManageCourseStudentsVm
            {
                CourseId = course.Id,
                CourseTitle = course.Title,
                Students = students.Select(s => new StudentPickVm
                {
                    StudentDbId = s.Id,
                    StudentIndex = s.StudentId,
                    FullName = s.FirstName + " " + s.LastName,
                    IsEnrolled = enrollmentMap.ContainsKey(s.Id),
                    EnrollmentId = enrollmentMap.ContainsKey(s.Id) ? enrollmentMap[s.Id].Id : null
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageStudents(ManageCourseStudentsVm vm)
        {
            var existing = await _context.Enrollments
                .Where(e => e.CourseId == vm.CourseId)
                .ToListAsync();

            var existingMap = existing.ToDictionary(e => e.StudentId, e => e);

            foreach (var s in vm.Students)
            {
                bool shouldEnroll = s.IsEnrolled;
                bool isEnrolledNow = existingMap.ContainsKey(s.StudentDbId);

                if (shouldEnroll && !isEnrolledNow)
                {
                    _context.Enrollments.Add(new Enrollment
                    {
                        CourseId = vm.CourseId,
                        StudentId = s.StudentDbId,
                        Year = DateTime.Now.Year
                    });
                }
                else if (!shouldEnroll && isEnrolledNow)
                {
                    _context.Enrollments.Remove(existingMap[s.StudentDbId]);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = vm.CourseId });
        }



        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName");
            return View();
        }

        // POST: Courses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.SecondTeacherId);
            return View(course);
        }

        // POST: Courses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Credits,Semester,Programme,EducationLevel,FirstTeacherId,SecondTeacherId")] Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(course);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["FirstTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.FirstTeacherId);
            ViewData["SecondTeacherId"] = new SelectList(_context.Teachers, "Id", "FirstName", course.SecondTeacherId);
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.FirstTeacher)
                .Include(c => c.SecondTeacher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        // POST: Courses/Delete/5
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
