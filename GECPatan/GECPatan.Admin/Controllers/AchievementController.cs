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
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class AchievementController : Controller
    {
        private const string UploadFolder = "achievements";

        // Fixed, small, and never queried from the DB — a static list avoids
        // rebuilding the same five SelectListItems on every request.
        private static readonly List<SelectListItem> AchievementTypes = new()
        {
            new("Academic", "1"), new("Sports", "2"),
            new("Cultural", "3"), new("NSS", "4"), new("Other", "5")
        };

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<AchievementController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId, int? committeeId, int? year)
        {
            ViewData["Title"] = "Achievements";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).AsNoTracking().ToListAsync(), "DeptId", "Name", deptId);
            ViewBag.Committees = new SelectList(
                await _context.CampusCommittees.OrderBy(c => c.Title).AsNoTracking().ToListAsync(), "Id", "Title", committeeId);

            var years = await _context.Achievements
                .Where(a => a.Year.HasValue && !a.IsDeleted)
                .Select(a => a.Year!.Value)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
            ViewBag.Years = new SelectList(years.Select(y => new { Value = y, Text = y.ToString() }), "Value", "Text", year);
            ViewBag.SelectedYear = year;

            var query = _context.Achievements.AsNoTracking().Where(a => !a.IsDeleted);
            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);
            if (committeeId.HasValue) query = query.Where(a => a.CommitteeId == committeeId);
            if (year.HasValue) query = query.Where(a => a.Year == year);

            var list = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new AchievementListVM
                {
                    Id = a.Id,
                    Title = a.Title,
                    Date = a.Date,
                    DeptName = a.DeptName,
                    Year = a.Year,
                    IsVisible = a.IsVisible,
                    ImagePath = a.ImagePath
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Achievement";
            return View(await BuildCreateVM(new AchievementCreateVM()));
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AchievementCreateVM model, IFormFile? Image)
        {
            ViewData["Title"] = "Add Achievement";

            // HOD can only publish achievements under their own department.
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            string? savedImagePath = null;
            if (Image != null && Image.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Image, UploadFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Image), uploadResult.ErrorMessage!);
                    return View(await BuildCreateVM(model));
                }
                savedImagePath = uploadResult.RelativePath;
            }

            try
            {
                var ach = new Achievement
                {
                    Title = model.Title,
                    Description = model.Description,
                    Date = model.Date,
                    Year = model.Year,
                    Keywords = model.Keywords,
                    Type = model.Type,
                    DeptId = model.DeptId,
                    DeptName = model.DeptName,
                    CommitteeId = model.CommitteeId,
                    IsVisible = model.IsVisible,
                    ImagePath = savedImagePath
                };

                _context.Achievements.Add(ach);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Achievement {Id} '{Title}' created by {User}", ach.Id, ach.Title, User.Identity?.Name);
                TempData["Success"] = "Achievement added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating achievement '{Title}'", model.Title);
                CleanupOrphanFile(savedImagePath);
                ModelState.AddModelError(string.Empty, "Could not save the achievement due to a database error. Please try again.");
                return View(await BuildCreateVM(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating achievement '{Title}'", model.Title);
                CleanupOrphanFile(savedImagePath);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the achievement. Please try again.");
                return View(await BuildCreateVM(model));
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Achievement";
            var a = await _context.Achievements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            var vm = new AchievementEditVM
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Date = a.Date,
                Year = a.Year,
                Keywords = a.Keywords,
                Type = a.Type,
                DeptId = a.DeptId,
                DeptName = a.DeptName,
                CommitteeId = a.CommitteeId,
                IsVisible = a.IsVisible,
                ExistingImagePath = a.ImagePath
            };

            return View(await BuildEditVM(vm));
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AchievementEditVM model, IFormFile? Image)
        {
            ViewData["Title"] = "Edit Achievement";

            var a = await _context.Achievements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                // Must own both the achievement's current department and
                // whatever department the form is trying to move it to.
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            string? newlySavedImage = null;
            string? oldImageToRemove = null;

            if (Image != null && Image.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Image, UploadFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Image), uploadResult.ErrorMessage!);
                    return View(await BuildEditVM(model));
                }
                newlySavedImage = uploadResult.RelativePath;
                oldImageToRemove = a.ImagePath;
                a.ImagePath = uploadResult.RelativePath;
            }

            a.Title = model.Title;
            a.Description = model.Description;
            a.Date = model.Date;
            a.Year = model.Year;
            a.Keywords = model.Keywords;
            a.Type = model.Type;
            a.DeptId = model.DeptId;
            a.DeptName = model.DeptName;
            a.CommitteeId = model.CommitteeId;
            a.IsVisible = model.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing achievement {Id}", id);
                CleanupOrphanFile(newlySavedImage);
                ModelState.AddModelError(string.Empty, "Could not save the achievement due to a database error. Please try again.");
                return View(await BuildEditVM(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing achievement {Id}", id);
                CleanupOrphanFile(newlySavedImage);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the achievement. Please try again.");
                return View(await BuildEditVM(model));
            }

            // Only remove the old image once the new state is safely
            // persisted, so a failed save never leaves the achievement with
            // no image at all.
            TryDeleteFile(oldImageToRemove, "replaced achievement image");

            _logger.LogInformation("Achievement {Id} '{Title}' edited by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Achievement updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.Achievements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
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
                _logger.LogError(ex, "Failed to toggle visibility for achievement {Id}", id);
                TempData["Error"] = "Could not update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Achievements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            a.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete achievement {Id}", id);
                TempData["Error"] = "Could not delete the achievement. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Achievement {Id} '{Title}' deleted by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Achievement deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// Shared by BuildCreateVM/BuildEditVM so the Departments/Committees
        /// dropdown queries exist in exactly one place.
        /// </summary>
        private async Task PopulateDropdowns(List<SelectListItem> departments, List<SelectListItem> committees)
        {
            departments.AddRange(await _context.Departments.OrderBy(d => d.Name).AsNoTracking()
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync());
            committees.AddRange(await _context.CampusCommittees.OrderBy(c => c.Title).AsNoTracking()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync());
        }

        private async Task<AchievementCreateVM> BuildCreateVM(AchievementCreateVM vm)
        {
            vm.Departments = new List<SelectListItem>();
            vm.Committees = new List<SelectListItem>();
            await PopulateDropdowns(vm.Departments, vm.Committees);
            vm.Types = AchievementTypes;
            return vm;
        }

        private async Task<AchievementEditVM> BuildEditVM(AchievementEditVM vm)
        {
            vm.Departments = new List<SelectListItem>();
            vm.Committees = new List<SelectListItem>();
            await PopulateDropdowns(vm.Departments, vm.Committees);
            vm.Types = AchievementTypes;
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