using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        private readonly IWebHostEnvironment _env;

        public DeptNoticeController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX — grouped by department ─────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Department Noticeboard";

            var currentUser = await GetCurrentUserAsync();
            var isAdmin = User.IsInRole("SuperAdmin") ||
                              User.IsInRole("Principal");

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

            var now = DateTime.Now;

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

            // Group by department
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
            bool isAdmin = User.IsInRole("SuperAdmin") ||
                              User.IsInRole("Principal");

            // Pre-select dept for HOD/Faculty
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
        public async Task<IActionResult> Create(
            DeptNoticeFormVM model, IFormFile? AttachFile)
        {
            ViewData["Title"] = "Add Notice";
            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") ||
                                User.IsInRole("Principal");

            // HOD/Faculty can only post for their dept
            if (!isAdmin && currentUser?.DeptId != null)
                model.DeptId = currentUser.DeptId.Value;

            ValidateForm(model, AttachFile);
            if (!ModelState.IsValid)
            {
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }

            var notice = new DeptNotice
            {
                DeptId = model.DeptId,
                Title = model.Title,
                Description = model.Description,
                FileType = model.FileType,
                ExternalLink = model.FileType == "Link"
                    ? model.ExternalLink : null,
                ValidFrom = model.ValidFrom,
                ValidTo = model.ValidTo,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder,
                PostedBy = User.Identity?.Name ?? "Admin"
            };

            if (AttachFile != null && AttachFile.Length > 0 &&
                model.FileType != "None" && model.FileType != "Link")
                notice.FilePath = await SaveFileAsync(AttachFile, "notices");

            _context.DeptNotices.Add(notice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Notice '{notice.Title}' added.";
            return RedirectToAction(nameof(Index),
                new { deptId = notice.DeptId });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Notice";
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            // HOD/Faculty can only edit their dept
            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") ||
                              User.IsInRole("Principal");

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
        public async Task<IActionResult> Edit(
            int id, DeptNoticeFormVM model, IFormFile? AttachFile)
        {
            ViewData["Title"] = "Edit Notice";
            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") ||
                                User.IsInRole("Principal");

            ValidateForm(model, AttachFile);
            if (!ModelState.IsValid)
            {
                await LoadDepartments(model, isAdmin, currentUser?.DeptId);
                return View("Form", model);
            }

            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            notice.Title = model.Title;
            notice.Description = model.Description;
            notice.FileType = model.FileType;
            notice.ExternalLink = model.FileType == "Link"
                ? model.ExternalLink : null;
            notice.ValidFrom = model.ValidFrom;
            notice.ValidTo = model.ValidTo;
            notice.IsVisible = model.IsVisible;
            notice.DisplayOrder = model.DisplayOrder;

            if (AttachFile != null && AttachFile.Length > 0 &&
                model.FileType != "None" && model.FileType != "Link")
            {
                DeleteFile(notice.FilePath);
                notice.FilePath = await SaveFileAsync(AttachFile, "notices");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Notice '{notice.Title}' updated.";
            return RedirectToAction(nameof(Index),
                new { deptId = notice.DeptId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();
            notice.IsVisible = !notice.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{notice.Title}' "
                + (notice.IsVisible ? "shown" : "hidden") + ".";
            return RedirectToAction(nameof(Index),
                new { deptId = notice.DeptId });
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isAdmin = User.IsInRole("SuperAdmin") ||
                              User.IsInRole("Principal");

            if (!isAdmin && currentUser?.DeptId != notice.DeptId)
                return Forbid();

            int deptId = notice.DeptId;
            DeleteFile(notice.FilePath);
            notice.IsDeleted = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Notice '{notice.Title}' deleted.";
            return RedirectToAction(nameof(Index), new { deptId });
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string dir)
        {
            var notice = await _context.DeptNotices.FindAsync(id);
            if (notice == null) return NotFound();

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

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { deptId = notice.DeptId });
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private void ValidateForm(DeptNoticeFormVM m, IFormFile? file)
        {
            if (m.ValidFrom.HasValue && m.ValidTo.HasValue
                && m.ValidTo < m.ValidFrom)
                ModelState.AddModelError("ValidTo",
                    "Valid To must be after Valid From.");

            if (m.FileType == "Link" &&
                string.IsNullOrWhiteSpace(m.ExternalLink))
                ModelState.AddModelError("ExternalLink",
                    "Please enter a URL for Link type.");

            if ((m.FileType == "PDF" || m.FileType == "Image") &&
                file == null && string.IsNullOrEmpty(m.ExistingFilePath))
                ModelState.AddModelError("AttachFile",
                    $"Please upload a {m.FileType} file.");
        }

        private async Task LoadDepartments(
            DeptNoticeFormVM vm, bool isAdmin, int? restrictedDeptId)
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
                var dept = await _context.Departments
                    .FindAsync(restrictedDeptId.Value);
                if (dept != null)
                    vm.Departments = new List<SelectListItem>
                    {
                        new() {
                            Value    = dept.DeptId.ToString(),
                            Text     = dept.Name,
                            Selected = true
                        }
                    };
            }
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private async Task<string> SaveFileAsync(
            IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(
                _env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid()
                + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(
                _env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}