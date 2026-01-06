using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using University_Management_Platform.Areas.Identity.Data;
using University_Management_Platform.Data;
using University_Management_Platform.Models;
using University_Management_Platform.Models.ViewModels;
using University_Management_Platform.ViewModels.Students;
using University_Management_Platform.ViewModels.Teachers;

namespace University_Management_Platform.Controllers
{
    public class StudentsController : Controller
    {
        private readonly UniversityDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<University_Management_PlatformUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public StudentsController(UniversityDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<University_Management_PlatformUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Students
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? studentId, string? firstName, string? lastName)
        {
            var q = _context.Students.AsQueryable();

            if (!string.IsNullOrWhiteSpace(studentId))
                q = q.Where(s => s.StudentId.Contains(studentId));

            if (!string.IsNullOrWhiteSpace(firstName))
                q = q.Where(s => s.FirstName.Contains(firstName));

            if (!string.IsNullOrWhiteSpace(lastName))
                q = q.Where(s => s.LastName.Contains(lastName));

            ViewBag.StudentId = studentId;
            ViewBag.FirstName = firstName;
            ViewBag.LastName = lastName;

            return View(await q.ToListAsync());
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (student == null) return NotFound();

            var vm = new StudentEditVM
            {
                Id = student.Id,
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                EnrollmentDate = student.EnrollmentDate,
                AccquiredCredits = student.AccquiredCredits,
                CurrentSemester = student.CurrentSemester,
                EducationLevel = student.EducationLevel,
                ExistingPhotoPath = student.photoPath
            };
            
            return View("Details", vm);
        }


        [Authorize(Roles = "Student")]
        public async Task<IActionResult> EditMyProfile()
        {
            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (student == null) return NotFound();

            var vm = new StudentEditVM
            {
                Id = student.Id,
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                EnrollmentDate = student.EnrollmentDate,
                AccquiredCredits = student.AccquiredCredits,
                CurrentSemester = student.CurrentSemester,
                EducationLevel = student.EducationLevel,
                ExistingPhotoPath = student.photoPath
            };

            return View("Edit", vm);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyProfile(StudentEditVM vm, [FromServices] IFileStorageService files)
        {
            if (!ModelState.IsValid)
                return View("Edit", vm);

            var userId = _userManager.GetUserId(User);

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == vm.Id && s.IdentityUserId == userId);

            if (student == null) return Forbid();

            student.StudentId = vm.StudentId;
            student.FirstName = vm.FirstName;
            student.LastName = vm.LastName;
            student.EnrollmentDate = vm.EnrollmentDate;
            student.AccquiredCredits = vm.AccquiredCredits;
            student.CurrentSemester = vm.CurrentSemester;
            student.EducationLevel = vm.EducationLevel;


            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(student.photoPath))
                    files.DeleteIfExists(student.photoPath, true);


                student.photoPath = await files.SavePhotoAsync(vm.Photo, "photos/students");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyProfile));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ByCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == courseId)
                .Include(e => e.Student)
                .ToListAsync();

            ViewBag.Course = course;
            return View(enrollments);
        }


        // GET: Students/Details/5
        [Authorize(Roles = "Admin, Student")]
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var s = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (s == null)
            {
                return NotFound();
            }

            var vm = new StudentEditVM
            {
                Id = s.Id,
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                EnrollmentDate = s.EnrollmentDate,
                AccquiredCredits = s.AccquiredCredits,
                CurrentSemester = s.CurrentSemester,
                EducationLevel = s.EducationLevel,
                ExistingPhotoPath = s.photoPath
            };

            return View(vm);
        }

        // GET: Students/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new CreateStudentVM());
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // ensure role exists
            if (!await _roleManager.RoleExistsAsync("Student"))
                await _roleManager.CreateAsync(new IdentityRole("Student"));

            // 1) create identity user
            var user = new University_Management_PlatformUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true
            };

            var res = await _userManager.CreateAsync(user, vm.Password);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // 2) assign role
            await _userManager.AddToRoleAsync(user, "Student");

            // 3) create teacher entity linked to identity
            var student = new Student
            {
                StudentId = vm.StudentId,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                EnrollmentDate = vm.EnrollmentDate,
                AccquiredCredits = vm.AccquiredCredits,
                CurrentSemester = vm.CurrentSemester,
                EducationLevel = vm.EducationLevel,
                IdentityUserId = user.Id
            };
            

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        [Authorize(Roles = "Admin, Student")]
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var s = await _context.Students.FindAsync(id);
            if (s == null)
            {
                return NotFound();
            }

            var vm = new StudentEditVM
            {
                Id = s.Id,
                StudentId = s.StudentId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                EnrollmentDate = s.EnrollmentDate,
                AccquiredCredits = s.AccquiredCredits,
                CurrentSemester = s.CurrentSemester,
                EducationLevel = s.EducationLevel,
                ExistingPhotoPath = s.photoPath
            };


            return View(vm);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StudentEditVM vm, [FromServices] IFileStorageService files)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var student = await _context.Students.FindAsync(vm.Id);
            if (student == null)
                return NotFound();

            student.StudentId = vm.StudentId;
            student.FirstName = vm.FirstName;
            student.LastName = vm.LastName;
            student.EnrollmentDate = vm.EnrollmentDate;
            student.AccquiredCredits = vm.AccquiredCredits;
            student.CurrentSemester = vm.CurrentSemester;
            student.EducationLevel = vm.EducationLevel;

            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(student.photoPath))
                    files.DeleteIfExists(student.photoPath, true);

                student.photoPath = await files.SavePhotoAsync(vm.Photo, "photos/students");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Students/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(long id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
