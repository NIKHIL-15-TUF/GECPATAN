using System.Security.Claims;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class DepartmentController : Controller
    {
        private const string UploadFolder = "departments";
        private const string AuditModule = "Department";
        private const string SuperAdminRole = "SuperAdmin";
        private const string DepartmentIcon = "fa-building";

        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DepartmentController> _logger;

        public DepartmentController(
            ApplicationDbContext context,
            NotificationService notify,
            IFileStorageService fileStorage,
            ILogger<DepartmentController> logger)
        {
            _context = context;
            _notify = notify;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Departments";

            var depts = await _context.Departments
                .Include(d => d.Faculties)
                .Include(d => d.Labs)
                .OrderBy(d => d.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();

            var intakes = await _context.ProgramIntakes
                .GroupBy(p => p.DeptId)
                .Select(g => new { DeptId = g.Key, Total = g.Sum(p => p.Intake) })
                .AsNoTracking()
                .ToListAsync();

            var list = depts.Select(d => new DepartmentListVM
            {
                DeptId = (int)d.DeptId,
                Name = d.Name,
                ShortCode = d.ShortCode,
                TitleImagePath = d.TitleImagePath,
                IsActive = d.IsActive,
                DisplayOrder = d.DisplayOrder,
                FacultyCount = d.Faculties.Count(f => f.IsActive),
                LabCount = d.Labs.Count,
                Intake = intakes.FirstOrDefault(i => i.DeptId == d.DeptId)?.Total ?? 0
            }).ToList();

            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────
        [Authorize(Roles = SuperAdminRole)]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Department";
            return View(new DepartmentCreateVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Create(DepartmentCreateVM model, IFormFile? TitleImage)
        {
            ViewData["Title"] = "Add Department";
            if (!ModelState.IsValid) return View(model);

            string? savedImagePath = null;

            if (TitleImage != null && TitleImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleImage, UploadFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                savedImagePath = uploadResult.RelativePath;
            }

            // Two SaveChangesAsync calls are needed — the audit entry wants the
            // department's real, database-generated DeptId, which only exists
            // after the first insert. Wrapping both in one transaction is what
            // actually gives us the atomicity guarantee (all-or-nothing), not
            // reducing to a single call.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var dept = new Department
                {
                    Name = model.Name,
                    ShortCode = model.ShortCode,
                    About = model.About,
                    Tagline = model.Tagline,
                    AnnualPlacement = model.AnnualPlacement,
                    ShowIntake = model.ShowIntake,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = true,
                    TitleImagePath = savedImagePath
                };

                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();

                AddAuditLog("Created", (int)dept.DeptId, dept.Name);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Department {DeptId} '{Name}' created by {User}", dept.DeptId, dept.Name, User.Identity?.Name);
                TempData["Success"] = $"Department '{dept.Name}' created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while creating department '{Name}'", model.Name);
                CleanupOrphanFile(savedImagePath);
                ModelState.AddModelError(string.Empty, "Could not save the department due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while creating department '{Name}'", model.Name);
                CleanupOrphanFile(savedImagePath);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the department. Please try again.");
                return View(model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Department";

            var d = await _context.Departments
                .Include(x => x.Faculties)
                .Include(x => x.Labs)
                .Include(x => x.BannerImages)
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.PEOs)
                .Include(x => x.PSOs)
                .FirstOrDefaultAsync(x => x.DeptId == id);

            if (d == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != id) return Forbid();
            }

            var intake = await _context.ProgramIntakes
                .Where(p => p.DeptId == id)
                .SumAsync(p => (int?)p.Intake) ?? 0;

            var vm = new DepartmentEditVM
            {
                DeptId = (int)d.DeptId,
                Name = d.Name,
                ShortCode = d.ShortCode,
                About = d.About,
                Tagline = d.Tagline,
                AnnualPlacement = d.AnnualPlacement,
                ShowIntake = d.ShowIntake,
                DisplayOrder = d.DisplayOrder,
                FacultyCount = d.Faculties.Count(f => f.IsActive),
                LabCount = d.Labs.Count,
                Intake = intake,
                ExistingTitleImagePath = d.TitleImagePath,
                ExistingBannerImages = d.BannerImages
                                          .Where(b => !b.IsDeleted)
                                          .OrderBy(b => b.DisplayOrder)
                                          .Select(b => new DepartmentBannerImageVM
                                          {
                                              Id = b.Id,
                                              ImagePath = b.ImagePath
                                          })
                                          .ToList(),
                VisionItems = d.Visions.OrderBy(v => v.DisplayOrder).Select(v => v.VisionText).ToList(),
                MissionItems = d.Missions.OrderBy(m => m.DisplayOrder).Select(m => m.MissionText).ToList(),
                PEOItems = d.PEOs.OrderBy(p => p.DisplayOrder).Select(p => p.PEOText).ToList(),
                PSOItems = d.PSOs.OrderBy(p => p.DisplayOrder).Select(p => p.PSOText).ToList()
            };

            return View(vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DepartmentEditVM model,
            IFormFile? TitleImage, List<IFormFile>? BannerImages,
            string? VisionItems, string? MissionItems,
            string? PEOItems, string? PSOItems)
        {
            ViewData["Title"] = "Edit Department";
            if (id != model.DeptId) return NotFound();
            if (!ModelState.IsValid) return View(model);

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != id) return Forbid();
            }

            var d = await _context.Departments
                .Include(x => x.BannerImages)
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.PEOs)
                .Include(x => x.PSOs)
                .FirstOrDefaultAsync(x => x.DeptId == id);

            if (d == null) return NotFound();

            // Files saved during this request. Tracked so they can be cleaned
            // up if SaveChangesAsync fails below — we never want an upload
            // sitting on disk that no database row points to.
            var newlySavedFiles = new List<string>();
            string? oldTitleImageToRemove = null;

            if (TitleImage != null && TitleImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleImage, UploadFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newlySavedFiles.Add(uploadResult.RelativePath!);
                oldTitleImageToRemove = d.TitleImagePath;
                d.TitleImagePath = uploadResult.RelativePath;
            }

            var skippedBanners = new List<string>();
            if (BannerImages != null && BannerImages.Any())
            {
                int order = d.BannerImages.Any()
                    ? d.BannerImages.Max(b => b.DisplayOrder) + 1 : 0;

                foreach (var img in BannerImages)
                {
                    if (img.Length == 0) continue;

                    var uploadResult = await _fileStorage.SaveAsync(img, UploadFolder, FileCategory.Image);
                    if (!uploadResult.Success)
                    {
                        // A multi-file picker can easily include one bad file among
                        // several good ones — skip just that file rather than
                        // discarding the whole edit, and tell the admin which
                        // file(s) were rejected and why.
                        skippedBanners.Add($"{img.FileName}: {uploadResult.ErrorMessage}");
                        continue;
                    }

                    newlySavedFiles.Add(uploadResult.RelativePath!);
                    _context.DepartmentImages.Add(new DepartmentBannerImage
                    {
                        DeptId = id,
                        ImagePath = uploadResult.RelativePath!,
                        DisplayOrder = order++
                    });
                }
            }

            d.Name = model.Name;
            d.ShortCode = model.ShortCode;
            d.About = model.About;
            d.Tagline = model.Tagline;
            d.AnnualPlacement = model.AnnualPlacement;
            d.ShowIntake = model.ShowIntake;
            d.DisplayOrder = model.DisplayOrder;

            ReplaceTextList(_context.DepartmentVisions, d.Visions, VisionItems,
                (text, order) => new DepartmentVision { DeptId = id, VisionText = text, DisplayOrder = order });

            ReplaceTextList(_context.DepartmentMissions, d.Missions, MissionItems,
                (text, order) => new DepartmentMission { DeptId = id, MissionText = text, DisplayOrder = order });

            ReplaceTextList(_context.DepartmentPEOs, d.PEOs, PEOItems,
                (text, order) => new DepartmentPEO { DeptId = id, PEOText = text, DisplayOrder = order });

            ReplaceTextList(_context.DepartmentPSOs, d.PSOs, PSOItems,
                (text, order) => new DepartmentPSO { DeptId = id, PSOText = text, DisplayOrder = order });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                AddAuditLog("Edited", (int)d.DeptId, d.Name);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while editing department {DeptId}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the department due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while editing department {DeptId}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the department. Please try again.");
                return View(model);
            }

            // Only remove the old title image once the new state is safely
            // persisted, so a failed save never leaves the department with
            // no image at all.
            TryDeleteFile(oldTitleImageToRemove, "replaced title image");

            _logger.LogInformation("Department {DeptId} '{Name}' edited by {User}", d.DeptId, d.Name, User.Identity?.Name);
            TempData["Success"] = $"Department '{d.Name}' updated.";
            if (skippedBanners.Any())
                TempData["Warning"] = "Some banner images were skipped: " + string.Join(" | ", skippedBanners);

            // Best-effort: the department is already saved at this point, so a
            // notification failure is logged, not surfaced as a request error.
            try
            {
                await _notify.SendAsync(
                    title: $"Department Updated: {model.Name}",
                    message: "HOD made changes to department profile",
                    module: AuditModule,
                    icon: DepartmentIcon,
                    color: "info",
                    link: $"/Department/Edit/{model.DeptId}",
                    forRole: SuperAdminRole
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send update notification for department {DeptId}", d.DeptId);
            }

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE BANNER IMAGE ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBannerImage(int id, int deptId)
        {
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != deptId) return Forbid();
            }

            var img = await _context.DepartmentImages.FindAsync(id);
            if (img == null || img.DeptId != deptId) return NotFound();

            var imagePath = img.ImagePath;

            try
            {
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete banner image {ImageId} for department {DeptId}", id, deptId);
                TempData["Error"] = "Could not delete the banner image. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = deptId });
            }

            // Only remove the physical file after the database change is
            // safely committed, so a failed save never leaves a dangling
            // reference to a file that no longer exists.
            TryDeleteFile(imagePath, "deleted banner image");
            return Ok();
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();

            d.IsActive = !d.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle active state for department {DeptId}", id);
                TempData["Error"] = "Could not update the department status. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"'{d.Name}' " + (d.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();

            d.IsDeleted = true;
            d.IsActive = false;

            try
            {
                AddAuditLog("Deleted", (int)d.DeptId, d.Name);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete department {DeptId}", id);
                TempData["Error"] = "Could not delete the department. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Department {DeptId} '{Name}' deleted by {User}", d.DeptId, d.Name, User.Identity?.Name);
            TempData["Success"] = $"Department '{d.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            // NOTE: this closes the "half-swapped" failure mode (a crash between
            // the two writes) via the transaction below, but does not add
            // optimistic-concurrency protection — two admins reordering at the
            // same instant can still race, since neither row carries a
            // concurrency token. Closing that fully needs a RowVersion column
            // added to Department (a model + migration change outside this file).
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var d = await _context.Departments.FindAsync(id);
                if (d == null) return NotFound();

                if (direction == "up")
                {
                    var above = await _context.Departments
                        .FirstOrDefaultAsync(x => x.DisplayOrder == d.DisplayOrder - 1);
                    if (above != null) { above.DisplayOrder++; d.DisplayOrder--; }
                }
                else
                {
                    var below = await _context.Departments
                        .FirstOrDefaultAsync(x => x.DisplayOrder == d.DisplayOrder + 1);
                    if (below != null) { below.DisplayOrder--; d.DisplayOrder++; }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to reorder department {DeptId}", id);
                TempData["Error"] = "Could not reorder departments. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// Replaces a department's child text-list collection (Vision, Mission,
        /// PEO, PSO items) with the newline-separated values submitted by the
        /// form. Shared by all four lists to avoid four copies of the same
        /// remove/split/re-add logic.
        /// </summary>
        private void ReplaceTextList<T>(
            DbSet<T> set,
            IEnumerable<T> existing,
            string? rawItems,
            Func<string, int, T> factory) where T : class
        {
            set.RemoveRange(existing);

            if (string.IsNullOrEmpty(rawItems)) return;

            var items = rawItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int order = 0;
            foreach (var raw in items)
            {
                var text = raw.Trim();
                if (text.Length == 0) continue;
                set.Add(factory(text, order++));
            }
        }

        /// <summary>
        /// Deletes a single file and never throws — used both for orphan
        /// cleanup after a failed save and for removing files that are no
        /// longer referenced after a successful one. A failed delete here
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

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDeleteFile(path, "orphan cleanup");
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// Stages an audit log entry on the context without saving. Callers
        /// commit it together with their own entity changes in a single
        /// SaveChangesAsync call, so the audit entry and the change it
        /// describes are always persisted atomically.
        /// </summary>
        private void AddAuditLog(string action, int recordId, string recordName)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = userName,
                UserRole = role,
                Action = action,
                Module = AuditModule,
                RecordId = recordId,
                RecordName = recordName,
                Timestamp = DateTime.UtcNow // stored in UTC; convert to local time when displaying
            });
        }
    }
}