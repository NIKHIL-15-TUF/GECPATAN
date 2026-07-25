using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,Faculty,Principal,ContentEditor")]
    public class DeptNoticeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<DeptNoticeController> _logger;
        private const string NoticesFolder = "notices";

        public DeptNoticeController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<DeptNoticeController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX — grouped by department ─────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Department Noticeboard";

            var currentUser = await GetCurrentUserAsync();
            var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            // HOD / Faculty — only see their dept
            if (!isAdmin && currentUser?.DeptId != null)
                deptId = currentUser.DeptId;

            // Dept filter dropdown (admin only)
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            ViewBag.SelectedDeptId = deptId;

            var query = _context.DeptNotices
                .Include(n => n.Department)
                .AsQueryable();

            if (deptId.HasValue)
                query = query.Where(n => n.DeptId == deptId.Value);
            else if (!isAdmin && currentUser?.DeptId != null)
                query = query.Where(n => n.DeptId == currentUser.DeptId);

            var notices = await query
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            var grouped = notices
                .GroupBy(n => new { n.DeptId, Name = n.Department?.Name ?? "" })
                .Select(g =>
                {
                    var list = g.Select(n => new DeptNoticeListVM
                    {
                        Id = n.Id,
                        DeptId = n.DeptId,
                        DeptName = n.Department?.Name ?? "",
                        Title = n.Title,
                        FileType = n.FileType,
                        FilePath = n.FilePath,
                        ExternalLink = n.ExternalLink,
                        ValidFrom = n.ValidFrom,
                        ValidTo = n.ValidTo,
                        PostedBy = n.PostedBy,
                        IsVisible = n.IsVisible,
                        DisplayOrder = n.DisplayOrder
                    }).ToList();

                    return new DeptNoticeBoardVM
                    {
                        DeptId = g.Key.DeptId,
                        DeptName = g.Key.Name,
                        Total = list.Count,
                        Active = list.Count(x => x.IsActive),
                        Expired = list.Count(x => x.IsExpired),
                        Notices = list
                    };
                })
                .OrderBy(g => g.DeptName)
                .ToList();

            return View(grouped);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Add Notice";
            var vm = new DeptNoticeFormVM
            {
                ValidFrom = DateTime.Today,
                ValidTo = DateTime.Today.AddDays(30)
            };

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != null)
                vm.DeptId = currentUser.DeptId.Value;
            else if (deptId.HasValue)
                vm.DeptId = deptId.Value;

            await LoadDepartments(vm, isAdmin, currentUser?.DeptId);
            return View("Form", vm);
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeptNoticeFormVM model, IFormFile? AttachFile)
        {
            ViewData["Title"] = "Add Notice";
            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            // HOD/Faculty can only post for their own dept
            if (!isAdmin && currentUser?.DeptId != null)
                model.DeptId = currentUser.DeptId.Value;

            ValidateForm(model, AttachFile);
            if (!ModelState.IsValid)
            {
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }

            string? savedFilePath = null;
            bool requiresFile = AttachFile != null && AttachFile.Length > 0 &&
                                 model.FileType != "None" && model.FileType != "Link";

            if (requiresFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    AttachFile, NoticesFolder, GetCategoryForFileType(model.FileType));

                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("AttachFile", uploadResult.ErrorMessage!);
                    await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                    return View("Form", model);
                }

                savedFilePath = uploadResult.RelativePath;
            }

            var notice = new DeptNotice
            {
                DeptId = model.DeptId,
                Title = model.Title,
                Description = model.Description,
                FileType = model.FileType,
                ExternalLink = model.FileType == "Link" ? model.ExternalLink : null,
                FilePath = savedFilePath,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder,
                PostedBy = User.Identity?.Name ?? "Admin"
            };

            try
            {
                _context.DeptNotices.Add(notice);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Notice {NoticeId} '{Title}' created for department {DeptId} by {User}",
                    notice.Id, notice.Title, notice.DeptId, notice.PostedBy);

                TempData["Success"] = $"Notice '{notice.Title}' added.";
                return RedirectToAction(nameof(Index), new { deptId = notice.DeptId });
            }
            catch (DbUpdateException ex)
            {
                // Prevent orphan file: the DB row never made it in.
                _fileStorage.Delete(savedFilePath);
                _logger.LogError(ex,
                    "Database error creating notice '{Title}' for department {DeptId}",
                    model.Title, model.DeptId);
                ModelState.AddModelError(string.Empty, "Unable to save the notice. Please try again.");
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(savedFilePath);
                _logger.LogError(ex,
                    "Unexpected error creating notice '{Title}' for department {DeptId}",
                    model.Title, model.DeptId);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Notice";
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            var vm = new DeptNoticeFormVM
            {
                Id = notice.Id,
                DeptId = notice.DeptId,
                Title = notice.Title,
                Description = notice.Description,
                FileType = notice.FileType ?? "None",
                ExistingFilePath = notice.FilePath,
                ExternalLink = notice.ExternalLink,
                ValidFrom = notice.ValidFrom,
                ValidTo = notice.ValidTo,
                IsVisible = notice.IsVisible,
                DisplayOrder = notice.DisplayOrder
            };

            await LoadDepartments(vm, isAdmin, currentUser?.DeptId);
            return View("Form", vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeptNoticeFormVM model, IFormFile? AttachFile)
        {
            ViewData["Title"] = "Edit Notice";
            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            ValidateForm(model, AttachFile);
            if (!ModelState.IsValid)
            {
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }

            string? newFilePath = null;
            bool replacingFile = AttachFile != null && AttachFile.Length > 0 &&
                                   model.FileType != "None" && model.FileType != "Link";

            if (replacingFile)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    AttachFile, NoticesFolder, GetCategoryForFileType(model.FileType));

                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("AttachFile", uploadResult.ErrorMessage!);
                    await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                    return View("Form", model);
                }

                newFilePath = uploadResult.RelativePath;
            }

            string? previousFilePath = notice.FilePath;

            notice.Title = model.Title;
            notice.Description = model.Description;
            notice.FileType = model.FileType;
            notice.ExternalLink = model.FileType == "Link" ? model.ExternalLink : null;
            notice.ValidFrom = model.ValidFrom;
            notice.ValidTo = model.ValidTo;
            notice.IsVisible = model.IsVisible;
            notice.DisplayOrder = model.DisplayOrder;

            if (replacingFile)
                notice.FilePath = newFilePath;

            try
            {
                await _context.SaveChangesAsync();

                // Only remove the old file once the new state is safely persisted,
                // so a failed save never leaves the notice pointing at a deleted file.
                if (replacingFile)
                    _fileStorage.Delete(previousFilePath);

                _logger.LogInformation("Notice {NoticeId} '{Title}' updated by {User}",
                    notice.Id, notice.Title, User.Identity?.Name ?? "Admin");

                TempData["Success"] = $"Notice '{notice.Title}' updated.";
                return RedirectToAction(nameof(Index), new { deptId = notice.DeptId });
            }
            catch (DbUpdateException ex)
            {
                // Prevent orphan file: DB update failed, so discard the newly uploaded file
                // and leave the previously-saved file untouched.
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Database error updating notice {NoticeId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the notice. Please try again.");
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }
            catch (Exception ex)
            {
                if (replacingFile)
                    _fileStorage.Delete(newFilePath);

                _logger.LogError(ex, "Unexpected error updating notice {NoticeId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            notice.IsVisible = !notice.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Notice {NoticeId} visibility set to {IsVisible} by {User}",
                    notice.Id, notice.IsVisible, User.Identity?.Name ?? "Admin");
                TempData["Success"] = $"'{notice.Title}' " + (notice.IsVisible ? "shown" : "hidden") + ".";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for notice {NoticeId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId = notice.DeptId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            int deptId = notice.DeptId;
            string? filePath = notice.FilePath;
            notice.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Only remove the physical file once the soft-delete is committed,
                // so a failed save never leaves an orphaned "deleted" file reference.
                _fileStorage.Delete(filePath);

                _logger.LogInformation("Notice {NoticeId} '{Title}' deleted by {User}",
                    notice.Id, notice.Title, User.Identity?.Name ?? "Admin");
                TempData["Success"] = $"Notice '{notice.Title}' deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting notice {NoticeId}", id);
                TempData["Error"] = "Unable to delete the notice. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string dir)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            var siblings = await _context.DeptNotices
                .Where(n => n.DeptId == notice.DeptId && !n.IsDeleted)
                .OrderBy(n => n.DisplayOrder)
                .ToListAsync();

            int idx = siblings.FindIndex(n => n.Id == id);

            if (dir == "up" && idx > 0)
            {
                siblings[idx].DisplayOrder--;
                siblings[idx - 1].DisplayOrder++;
            }
            else if (dir == "down" && idx < siblings.Count - 1)
            {
                siblings[idx].DisplayOrder++;
                siblings[idx + 1].DisplayOrder--;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering notice {NoticeId}", id);
                TempData["Error"] = "Unable to reorder notices. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId = notice.DeptId });
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private void ValidateForm(DeptNoticeFormVM m, IFormFile? file)
        {
            if (m.ValidFrom.HasValue && m.ValidTo.HasValue && m.ValidTo < m.ValidFrom)
                ModelState.AddModelError("ValidTo", "Valid To must be after Valid From.");

            if (m.FileType == "Link" && string.IsNullOrWhiteSpace(m.ExternalLink))
                ModelState.AddModelError("ExternalLink", "Please enter a URL for Link type.");

            if ((m.FileType == "PDF" || m.FileType == "Image") &&
                file == null && string.IsNullOrEmpty(m.ExistingFilePath))
                ModelState.AddModelError("AttachFile", $"Please upload a {m.FileType} file.");
        }

        private static FileCategory GetCategoryForFileType(string fileType) =>
            fileType == "Image" ? FileCategory.Image : FileCategory.Document;

        private async Task LoadDepartments(DeptNoticeFormVM vm, bool isAdmin, int? restrictedDeptId)
        {
            if (isAdmin)
            {
                vm.Departments = await _context.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.DisplayOrder)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DeptId.ToString(),
                        Text = d.Name
                    }).ToListAsync();
            }
            else if (restrictedDeptId.HasValue)
            {
                var dept = await _context.Departments.FindAsync(restrictedDeptId.Value);
                if (dept != null)
                    vm.Departments = new List<SelectListItem>
                    {
                        new() { Value = dept.DeptId.ToString(), Text = dept.Name, Selected = true }
                    };
            }
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}