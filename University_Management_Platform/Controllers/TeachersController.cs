using Microsoft.AspNetCore.Authorization;
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
    public class TeachersController : Controller
    {
        private readonly UniversityDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<University_Management_PlatformUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TeachersController(UniversityDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<University_Management_PlatformUser> userManager,RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Teachers
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> Index(string? firstName, string? lastName, string? degree, string? academicRank)
        {
            var q = _context.Teachers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(firstName))
                q = q.Where(t => t.FirstName.Contains(firstName));

            if (!string.IsNullOrWhiteSpace(lastName))
                q = q.Where(t => t.LastName.Contains(lastName));

            if (!string.IsNullOrWhiteSpace(degree))
                q = q.Where(t => t.Degree != null && t.Degree.Contains(degree));

            if (!string.IsNullOrWhiteSpace(academicRank))
                q = q.Where(t => t.AcademicRank != null && t.AcademicRank.Contains(academicRank));

            ViewBag.FirstName = firstName;
            ViewBag.LastName = lastName;
            ViewBag.Degree = degree;
            ViewBag.AcademicRank = academicRank;

            return View(await q.ToListAsync());
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            var vm = new TeachersEditVM
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                Degree = teacher.Degree,
                AcademicRank = teacher.AcademicRank,
                OfficeNumber = teacher.OfficeNumber,
                HireDate = teacher.HireDate,
                ExistingPhotoPath = teacher.PhotoPath
            };
            return View("Details", vm);
        }


        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> EditMyProfile()
        {
            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdentityUserId == userId);

            if (teacher == null) return NotFound();

            var vm = new TeachersEditVM
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                Degree = teacher.Degree,
                AcademicRank = teacher.AcademicRank,
                OfficeNumber = teacher.OfficeNumber,
                HireDate = teacher.HireDate,
                ExistingPhotoPath = teacher.PhotoPath
            };

            return View("Edit",vm);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMyProfile(TeachersEditVM vm, [FromServices] IFileStorageService files)
        {
            if (!ModelState.IsValid)
                return View("Edit", vm);

            var userId = _userManager.GetUserId(User);

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == vm.Id && t.IdentityUserId == userId);

            if (teacher == null) return Forbid();

            teacher.FirstName = vm.FirstName;
            teacher.LastName = vm.LastName;
            teacher.Degree = vm.Degree;
            teacher.AcademicRank = vm.AcademicRank;
            teacher.OfficeNumber = vm.OfficeNumber;
            teacher.HireDate = vm.HireDate;

            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(teacher.PhotoPath))
                    files.DeleteIfExists(teacher.PhotoPath, true);

                
                teacher.PhotoPath = await files.SavePhotoAsync(vm.Photo, "photos/teachers");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyProfile));
        }


        // GET: Teachers/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var teacher = await _context.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id.Value);

            if (teacher == null)
                return NotFound();

            ViewBag.Courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.FirstTeacherId == id.Value || c.SecondTeacherId == id.Value)
                .ToListAsync();

            var vm = new TeachersEditVM
            {
                Id = teacher.Id,
                FirstName = teacher.FirstName,
                LastName = teacher.LastName,
                Degree = teacher.Degree,
                AcademicRank = teacher.AcademicRank,
                OfficeNumber = teacher.OfficeNumber,
                HireDate = teacher.HireDate,
                ExistingPhotoPath = teacher.PhotoPath 
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new CreateTeacherVM());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTeacherVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // ensure role exists
            if (!await _roleManager.RoleExistsAsync("Teacher"))
                await _roleManager.CreateAsync(new IdentityRole("Teacher"));

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
            await _userManager.AddToRoleAsync(user, "Teacher");

            // 3) create teacher entity linked to identity
            var teacher = new Teacher
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Degree = vm.Degree,
                AcademicRank = vm.AcademicRank,
                OfficeNumber = vm.OfficeNumber,
                HireDate = vm.HireDate,
                IdentityUserId = user.Id
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Teachers/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var t = await _context.Teachers.FindAsync(id);
            if (t == null)
            {
                return NotFound();
            }

            var vm = new TeachersEditVM
            {
                Id = t.Id,
                FirstName = t.FirstName,
                LastName = t.LastName,
                Degree = t.Degree,
                AcademicRank = t.AcademicRank,
                OfficeNumber = t.OfficeNumber,
                HireDate = t.HireDate,
                ExistingPhotoPath = t.PhotoPath
            };

            return View(vm);
        }

        // POST: Teachers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeachersEditVM vm, [FromServices] IFileStorageService files)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var teacher = await _context.Teachers.FindAsync(vm.Id);
            if (teacher == null)
                return NotFound();

            teacher.FirstName = vm.FirstName;
            teacher.LastName = vm.LastName;
            teacher.Degree = vm.Degree;
            teacher.AcademicRank = vm.AcademicRank;
            teacher.OfficeNumber = vm.OfficeNumber;
            teacher.HireDate = vm.HireDate;


            if (vm.Photo != null && vm.Photo.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(teacher.PhotoPath))
                    files.DeleteIfExists(teacher.PhotoPath, true);

                teacher.PhotoPath = await files.SavePhotoAsync(vm.Photo, "photos/students");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Teachers/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teachers/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null) return RedirectToAction(nameof(Index));

            // delete identity user too
            if (!string.IsNullOrWhiteSpace(teacher.IdentityUserId))
            {
                var user = await _userManager.FindByIdAsync(teacher.IdentityUserId);
                if (user != null)
                    await _userManager.DeleteAsync(user);
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
