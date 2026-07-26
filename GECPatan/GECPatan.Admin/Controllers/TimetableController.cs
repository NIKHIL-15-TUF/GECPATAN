using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor")]
    public class TimetableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<TimetableController> _logger;
        private const string TimetablesFolder = "timetables";

        public TimetableController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<TimetableController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId, int? year)
        {
            ViewData["Title"] = "Timetables";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var years = await _context.Timetables
                .Select(t => t.Year).Distinct()
                .OrderByDescending(y => y).ToListAsync();

            if (!years.Contains(DateTime.Today.Year))
                years.Insert(0, DateTime.Today.Year);

            ViewBag.Years = new SelectList(years, year);
            ViewBag.SelectedYear = year;

            var query = _context.Timetables
                .Include(t => t.Department)
                .AsQueryable();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != null)
                    query = query.Where(t => t.DeptId == currentUser.DeptId);
            }
            else if (deptId.HasValue)
                query = query.Where(t => t.DeptId == deptId.Value);

            if (year.HasValue)
                query = query.Where(t => t.Year == year.Value);

            var list = await query
                .OrderByDescending(t => t.UploadedDate)
                .Select(t => new TimetableListVM
                {
                    Id = t.Id,
                    DeptName = t.Department != null ? t.Department.Name : "",
                    DeptId = t.DeptId,
                    Year = t.Year,
                    Semester = t.Semester,
                    SemesterType = t.SemesterType,
                    FilePath = t.FilePath,
                    IsVisible = t.IsVisible,
                    IsLatest = t.IsLatest,
                    UploadedDate = t.UploadedDate
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Upload Timetable";

            var currentUser = await GetCurrentUserAsync();
            var vm = new TimetableCreateVM
            {
                DeptId = User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != null
                    ? currentUser.DeptId.Value
                    : deptId ?? 0
            };
            return View(await BuildVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimetableCreateVM model, IFormFile? TimetableFile)
        {
            ViewData["Title"] = "Upload Timetable";

            var currentUser = await GetCurrentUserAsync();

            // HOD can only upload timetables for their own department
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != null)
                model.DeptId = currentUser.DeptId.Value;

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            if (TimetableFile == null || TimetableFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload.");
                return View(await BuildVM(model));
            }

            var uploadResult = await _fileStorage.SaveAsync(TimetableFile, TimetablesFolder, FileCategory.Document);
            if (!uploadResult.Success)
            {
                ModelState.AddModelError(nameof(TimetableFile), uploadResult.ErrorMessage!);
                return View(await BuildVM(model));
            }

            var filePath = uploadResult.RelativePath;

            var tt = new Timetable
            {
                DeptId = model.DeptId,
                Year = model.Year,
                SemesterType = model.SemesterType,
                Semester = model.Semester,
                IsVisible = model.IsVisible,
                IsLatest = true,
                UploadedDate = DateTime.Now,
                UploadedBy = User.Identity?.Name,
                FilePath = filePath
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mark previous timetable for same dept+sem+year as not latest
                var previous = await _context.Timetables
                    .Where(t => t.DeptId == model.DeptId &&
                                t.Semester == model.Semester &&
                                t.Year == model.Year &&
                                t.IsLatest)
                    .ToListAsync();

                foreach (var p in previous)
                    p.IsLatest = false;

                _context.Timetables.Add(tt);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Timetable {TimetableId} uploaded for dept {DeptId}, year {Year}, semester {Semester}",
                    tt.Id, tt.DeptId, tt.Year, tt.Semester);

                TempData["Success"] = "Timetable uploaded successfully.";
                return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _fileStorage.Delete(filePath);

                _logger.LogError(ex, "Database error uploading timetable for dept {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "Unable to save the timetable. Please try again.");
                return View(await BuildVM(model));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _fileStorage.Delete(filePath);

                _logger.LogError(ex, "Unexpected error uploading timetable for dept {DeptId}", model.DeptId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(await BuildVM(model));
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var tt = await _context.Timetables.FindAsync(id);
            if (tt == null) return NotFound();

            if (!await CanAccessAsync(tt.DeptId))
                return Forbid();

            tt.IsVisible = !tt.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Timetable {TimetableId} visibility set to {IsVisible}", id, tt.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for timetable {TimetableId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE (only non-latest can be deleted, and only by SuperAdmin)
        // Latest file is KEPT as history — never deleted
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tt = await _context.Timetables.FindAsync(id);
            if (tt == null) return NotFound();

            if (tt.IsLatest)
            {
                TempData["Error"] = "Cannot delete the latest timetable. Upload a new one to replace it.";
                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole(AppRoles.SuperAdmin))
            {
                TempData["Error"] = "Only SuperAdmin can delete timetable history.";
                return RedirectToAction(nameof(Index));
            }

            string? filePath = tt.FilePath;
            tt.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(filePath);

                _logger.LogInformation("Timetable {TimetableId} (history) deleted", id);
                TempData["Success"] = "Old timetable deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting timetable {TimetableId}", id);
                TempData["Error"] = "Unable to delete the timetable. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<TimetableCreateVM> BuildVM(TimetableCreateVM vm)
        {
            vm.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();
            return vm;
        }

        /// <summary>
        /// SuperAdmin and ContentEditor can access any department's timetables.
        /// HOD is restricted to their own department.
        /// </summary>
        private async Task<bool> CanAccessAsync(int deptId)
        {
            if (!User.IsInRole(AppRoles.HOD)) return true;

            var currentUser = await GetCurrentUserAsync();
            return currentUser?.DeptId == deptId;
        }

        private Task<ApplicationUser?> GetCurrentUserAsync() =>
            _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
    }
}