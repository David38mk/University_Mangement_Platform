using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using University_Management_Platform.Data;
using University_Management_Platform.Models;
using University_Management_Platform.ViewModels.EnrollmentSubmissions;

namespace University_Management_Platform.Controllers
{
    public class EnrollmentSubmissionsController : Controller
    {
        private readonly UniversityDbContext _context;
        private readonly IFileStorageService _fileStorage;

        private const string Subfolder = "enrollment-submissions";

        public EnrollmentSubmissionsController(UniversityDbContext context, IFileStorageService fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        // GET: /EnrollmentSubmissions?enrollmentId=5
        public async Task<IActionResult> Index(int enrollmentId)
        {
            var enrollmentExists = await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId);
            if (!enrollmentExists) return NotFound();

            var items = await _context.EnrollmentSubmissions
                .Where(s => s.EnrollmentID == enrollmentId)
                .OrderByDescending(s => s.UploadedAt)
                .ToListAsync();

            ViewBag.EnrollmentId = enrollmentId;
            return View(items);
        }

        // GET: /EnrollmentSubmissions/Create?enrollmentId=5
        public async Task<IActionResult> Create(int enrollmentId)
        {
            var enrollmentExists = await _context.Enrollments.AnyAsync(e => e.Id == enrollmentId);
            if (!enrollmentExists) return NotFound();

            var vm = new EnrollmentSubmissionsCreateVM
            {
                EnrollmentID = enrollmentId
            };

            return View(vm);
        }

        // POST: /EnrollmentSubmissions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EnrollmentSubmissionsCreateVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var enrollmentExists = await _context.Enrollments.AnyAsync(e => e.Id == vm.EnrollmentID);
            if (!enrollmentExists) return NotFound();

            try
            {
                // 1) Save doc (service validates ext+size)
                var (storedName, contentType, size) =
                    await _fileStorage.SaveDocumentAsync(vm.File, Subfolder);

                // 2) Versioning (Enrollment + SubmissionType)
                var last = await _context.EnrollmentSubmissions
                    .Where(s => s.EnrollmentID == vm.EnrollmentID &&
                                s.SubmissionType == vm.SubmissionType)
                    .OrderByDescending(s => s.Version)
                    .FirstOrDefaultAsync();

                var newVersion = (last?.Version ?? 0) + 1;

                // 3) Mark previous latest=false
                var latestOnes = await _context.EnrollmentSubmissions
                    .Where(s => s.EnrollmentID == vm.EnrollmentID &&
                                s.SubmissionType == vm.SubmissionType &&
                                s.IsLatest)
                    .ToListAsync();

                foreach (var l in latestOnes)
                    l.IsLatest = false;

                // 4) DB record
                var entity = new EnrollmentSubmission
                {
                    EnrollmentID = vm.EnrollmentID,
                    SubmissionType = vm.SubmissionType,
                    OriginalFileName = Path.GetFileName(vm.File.FileName),
                    StoredFileName = storedName,
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    FileSize = size,
                    UploadedAt = DateTime.UtcNow,
                    Version = newVersion,
                    IsLatest = true,
                    Comment = vm.Comment
                };

                _context.EnrollmentSubmissions.Add(entity);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { enrollmentId = vm.EnrollmentID });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        // GET: /EnrollmentSubmissions/Download/12
        public async Task<IActionResult> Download(int id)
        {
            var sub = await _context.EnrollmentSubmissions
                .FirstOrDefaultAsync(x => x.ID == id);

            if (sub == null) return NotFound();

            // Патеката мора да одговара со SaveDocumentAsync:
            // ContentRoot/App_Data/uploads/{subfolder}/{storedName}
            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data", "uploads", Subfolder, sub.StoredFileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("Фајлот не постои на диск.");

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var ct = string.IsNullOrWhiteSpace(sub.ContentType) ? "application/octet-stream" : sub.ContentType;

            return File(stream, ct, sub.OriginalFileName);
        }

        // POST: /EnrollmentSubmissions/Delete/12
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var sub = await _context.EnrollmentSubmissions
                .FirstOrDefaultAsync(x => x.ID == id);

            if (sub == null) return NotFound();

            var enrollmentId = sub.EnrollmentID;

            // патека како SaveDocumentAsync
            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data", "uploads", Subfolder, sub.StoredFileName);

            _context.EnrollmentSubmissions.Remove(sub);
            await _context.SaveChangesAsync();

            // delete file using existing service method (physical path => isRelativeWebPath:false)
            _fileStorage.DeleteIfExists(fullPath, isRelativeWebPath: false);

            return RedirectToAction(nameof(Index), new { enrollmentId });
        }
    }
}
