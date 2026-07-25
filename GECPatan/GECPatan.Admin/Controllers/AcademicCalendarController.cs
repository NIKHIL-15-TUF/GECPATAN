using System.Security.Claims;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor")]
    public class AcademicCalendarController : Controller
    {
        private const string UploadFolder = "academics";

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AcademicCalendarController> _logger;

        public AcademicCalendarController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<AcademicCalendarController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Academic Calendars";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).AsNoTracking().ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var query = _context.AcademicCalendars.AsNoTracking().Where(a => !a.IsDeleted);
            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);

            var list = await query
                .OrderBy(a => a.DisplayOrder)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var depts = await _context.Departments.AsNoTracking().ToDictionaryAsync(d => d.DeptId, d => d.Name);

            var vms = list.Select(a => new AcademicCalendarListVM
            {
                Id = a.Id,
                Title = a.Title,
                UploadDate = a.UploadDate,
                DeptName = a.DeptId.HasValue && depts.ContainsKey(a.DeptId.Value)
                             ? depts[a.DeptId.Value] : "All Departments",
                IsVisible = a.IsVisible,
                FilePath = a.FilePath
            }).ToList();

            return View(vms);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Academic Calendar";
            return View(await BuildVM(new AcademicCalendarVM
            {
                UploadDate = DateTime.Today.ToString("dd MMM yyyy")
            }));
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicCalendarVM model, IFormFile? CalFile)
        {
            ViewData["Title"] = "Add Academic Calendar";

            // HOD can only publish calendars for their own department, never
            // for "All Departments" (null) or another department.
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            string? savedFilePath = null;
            if (CalFile != null && CalFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(CalFile, UploadFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(CalFile), uploadResult.ErrorMessage!);
                    return View(await BuildVM(model));
                }
                savedFilePath = uploadResult.RelativePath;
            }

            try
            {
                int maxOrder = await _context.AcademicCalendars
                    .Select(a => (int?)a.DisplayOrder).MaxAsync() ?? -1;

                var ac = new AcademicCalendar
                {
                    Title = model.Title,
                    UploadDate = model.UploadDate,
                    DeptId = model.DeptId,
                    IsVisible = model.IsVisible,
                    DisplayOrder = maxOrder + 1,
                    FilePath = savedFilePath
                };

                _context.AcademicCalendars.Add(ac);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Academic calendar {Id} '{Title}' created by {User}", ac.Id, ac.Title, User.Identity?.Name);
                TempData["Success"] = "Academic Calendar added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating academic calendar '{Title}'", model.Title);
                CleanupOrphanFile(savedFilePath);
                ModelState.AddModelError(string.Empty, "Could not save the academic calendar due to a database error. Please try again.");
                return View(await BuildVM(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating academic calendar '{Title}'", model.Title);
                CleanupOrphanFile(savedFilePath);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the academic calendar. Please try again.");
                return View(await BuildVM(model));
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Academic Calendar";
            var a = await _context.AcademicCalendars.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            return View(await BuildVM(new AcademicCalendarVM
            {
                Id = a.Id,
                Title = a.Title,
                UploadDate = a.UploadDate,
                DeptId = a.DeptId,
                IsVisible = a.IsVisible,
                DisplayOrder = a.DisplayOrder,
                ExistingFilePath = a.FilePath
            }));
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicCalendarVM model, IFormFile? CalFile)
        {
            ViewData["Title"] = "Edit Academic Calendar";

            var a = await _context.AcademicCalendars.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                // Must own both the calendar's current department and whatever
                // department the form is trying to move it to.
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            string? newlySavedFile = null;
            string? oldFileToRemove = null;

            if (CalFile != null && CalFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(CalFile, UploadFolder, FileCategory.Document);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(CalFile), uploadResult.ErrorMessage!);
                    return View(await BuildVM(model));
                }
                newlySavedFile = uploadResult.RelativePath;
                oldFileToRemove = a.FilePath;
                a.FilePath = uploadResult.RelativePath;
            }

            a.Title = model.Title;
            a.UploadDate = model.UploadDate;
            a.DeptId = model.DeptId;
            a.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing academic calendar {Id}", id);
                CleanupOrphanFile(newlySavedFile);
                ModelState.AddModelError(string.Empty, "Could not save the academic calendar due to a database error. Please try again.");
                return View(await BuildVM(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing academic calendar {Id}", id);
                CleanupOrphanFile(newlySavedFile);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the academic calendar. Please try again.");
                return View(await BuildVM(model));
            }

            // Only remove the old file once the new state is safely persisted,
            // so a failed save never leaves the record with no file at all.
            TryDeleteFile(oldFileToRemove, "replaced calendar file");

            _logger.LogInformation("Academic calendar {Id} '{Title}' edited by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Academic Calendar updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.AcademicCalendars.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            a.IsVisible = !a.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle visibility for academic calendar {Id}", id);
                TempData["Error"] = "Could not update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.AcademicCalendars.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            var filePath = a.FilePath;
            a.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete academic calendar {Id}", id);
                TempData["Error"] = "Could not delete the academic calendar. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            // Only remove the physical file after the soft-delete is safely
            // committed, so a failed save never leaves a dangling reference.
            TryDeleteFile(filePath, "deleted academic calendar");

            _logger.LogInformation("Academic calendar {Id} '{Title}' deleted by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<AcademicCalendarVM> BuildVM(AcademicCalendarVM vm)
        {
            vm.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .AsNoTracking()
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();
            return vm;
        }

        /// <summary>
        /// Deletes a single file and never throws — a failed delete here
        /// should never take down the request; it just gets logged so it can
        /// be cleaned up manually.
        /// </summary>
        private void TryDeleteFile(string? path, string context)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (!_fileStorage.Delete(path))
                    _logger.LogWarning("File delete returned false for {Path} ({Context})", path, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {Path} ({Context})", path, context);
            }
        }

        private void CleanupOrphanFile(string? path) => TryDeleteFile(path, "orphan cleanup");

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId);
        }
    }
}