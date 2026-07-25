using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly NotificationService _notify;
        public DepartmentController(ApplicationDbContext context, IWebHostEnvironment env,NotificationService notify)
        {
            _context = context;
            _env = env;
            _notify = notify;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Departments";

            var depts = await _context.Departments
                .Include(d => d.Faculties)
                .Include(d => d.Labs)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            // Get intake per dept (latest year)
            var intakes = await _context.ProgramIntakes
                .GroupBy(p => p.DeptId)
                .Select(g => new { DeptId = g.Key, Total = g.Sum(p => p.Intake) })
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
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Department";
            return View(new DepartmentCreateVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(DepartmentCreateVM model, IFormFile? TitleImage)
        {
            ViewData["Title"] = "Add Department";
            if (!ModelState.IsValid) return View(model);

            var dept = new Department
            {
                Name = model.Name,
                ShortCode = model.ShortCode,
                About = model.About,
                Tagline = model.Tagline,
                AnnualPlacement = model.AnnualPlacement,
                ShowIntake = model.ShowIntake,
                DisplayOrder = model.DisplayOrder,
                IsActive = true
            };

            if (TitleImage != null && TitleImage.Length > 0)
                dept.TitleImagePath = await SaveFileAsync(TitleImage, "departments");

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            // Audit log
            await WriteAuditLog("Created", "Department", (int)dept.DeptId,dept.Name);
            TempData["Success"] = $"Department '{dept.Name}' created.";
            return RedirectToAction(nameof(Index));
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

            // HOD can only edit their own dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != id) return Forbid();
            }

            // Auto-calculate intake for latest year
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
                                          .OrderBy(b => b.DisplayOrder)
                                          .Select(b => b.ImagePath)
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

            var d = await _context.Departments
                .Include(x => x.BannerImages)
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.PEOs)
                .Include(x => x.PSOs)
                .FirstOrDefaultAsync(x => x.DeptId == id);

            if (d == null) return NotFound();

            d.Name = model.Name;
            d.ShortCode = model.ShortCode;
            d.About = model.About;
            d.Tagline = model.Tagline;
            d.AnnualPlacement = model.AnnualPlacement;
            d.ShowIntake = model.ShowIntake;
            d.DisplayOrder = model.DisplayOrder;

            // Title image
            if (TitleImage != null && TitleImage.Length > 0)
            {
                DeleteFile(d.TitleImagePath);
                d.TitleImagePath = await SaveFileAsync(TitleImage, "departments");
            }

            // Banner images — add new ones
            if (BannerImages != null && BannerImages.Any())
            {
                int order = d.BannerImages.Any()
                    ? d.BannerImages.Max(b => b.DisplayOrder) + 1 : 0;

                foreach (var img in BannerImages)
                {
                    if (img.Length > 0)
                    {
                        _context.DepartmentImages.Add(new DepartmentBannerImage
                        {
                            DeptId = id,
                            ImagePath = await SaveFileAsync(img, "departments"),
                            DisplayOrder = order++
                        });
                    }
                }
            }

            // Vision
            _context.DepartmentVisions.RemoveRange(d.Visions);
            if (!string.IsNullOrEmpty(VisionItems))
            {
                var items = VisionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i++)
                    if (!string.IsNullOrWhiteSpace(items[i]))
                        _context.DepartmentVisions.Add(new DepartmentVision
                        {
                            DeptId = id,
                            VisionText = items[i].Trim(),
                            DisplayOrder = i
                        });
            }

            // Mission
            _context.DepartmentMissions.RemoveRange(d.Missions);
            if (!string.IsNullOrEmpty(MissionItems))
            {
                var items = MissionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i++)
                    if (!string.IsNullOrWhiteSpace(items[i]))
                        _context.DepartmentMissions.Add(new DepartmentMission
                        {
                            DeptId = id,
                            MissionText = items[i].Trim(),
                            DisplayOrder = i
                        });
            }

            // PEOs
            _context.DepartmentPEOs.RemoveRange(d.PEOs);
            if (!string.IsNullOrEmpty(PEOItems))
            {
                var items = PEOItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i++)
                    if (!string.IsNullOrWhiteSpace(items[i]))
                        _context.DepartmentPEOs.Add(new DepartmentPEO
                        {
                            DeptId = id,
                            PEOText = items[i].Trim(),
                            DisplayOrder = i
                        });
            }

            // PSOs
            _context.DepartmentPSOs.RemoveRange(d.PSOs);
            if (!string.IsNullOrEmpty(PSOItems))
            {
                var items = PSOItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i++)
                    if (!string.IsNullOrWhiteSpace(items[i]))
                        _context.DepartmentPSOs.Add(new DepartmentPSO
                        {
                            DeptId = id,
                            PSOText = items[i].Trim(),
                            DisplayOrder = i
                        });
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw;
            }
            // Audit log
            await WriteAuditLog("Edited", "Department", (int)d.DeptId, d.Name);
            TempData["Success"] = $"Department '{d.Name}' updated.";
            //Notification Sends
            await _notify.SendAsync(
                title: $"Department Updated: {model.Name}",
                message: "HOD made changes to department profile",
                module: "Department",
                icon: "fa-building",
                color: "info",
                link: $"/Department/Edit/{model.DeptId}",
                forRole: "SuperAdmin"
            );
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE BANNER IMAGE ───────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteBannerImage(int id, int deptId)
        {
            var img = await _context.DepartmentImages.FindAsync(id);
            if (img != null)
            {
                DeleteFile(img.ImagePath);
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = deptId });
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();
            d.IsActive = !d.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{d.Name}' " + (d.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();
            d.IsDeleted = true;
            d.IsActive = false;
            await _context.SaveChangesAsync();
            await WriteAuditLog("Deleted", "Department", (int)d.DeptId, d.Name);
            TempData["Success"] = $"Department '{d.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.Departments
                    .Where(x => x.DisplayOrder == d.DisplayOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; d.DisplayOrder--; }
            }
            else
            {
                var below = await _context.Departments
                    .Where(x => x.DisplayOrder == d.DisplayOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; d.DisplayOrder++; }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        } 

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId);
        }
        private async Task WriteAuditLog(string action, string module, int recordId, string recordName)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var role = User.Claims
                .FirstOrDefault(c => c.Type ==
                    System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserName = userName,
                UserRole = role,
                Action = action,
                Module = module,
                RecordId = recordId,
                RecordName = recordName,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}