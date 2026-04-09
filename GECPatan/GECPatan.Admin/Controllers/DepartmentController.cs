using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal,HOD")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DepartmentController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Departments";

            var departments = await _context.Departments
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .Select(d => new DepartmentListVM
                {
                    DeptId = d.DeptId,
                    Name = d.Name,
                    ShortCode = d.ShortCode,
                    Intake = d.Intake,
                    FacultyCount = d.FacultyCount,
                    IsActive = d.IsActive,
                    DisplayOrder = d.DisplayOrder,
                    TitleImagePath = d.TitleImagePath
                })
                .ToListAsync();

            return View(departments);
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
        public async Task<IActionResult> Create(
            DepartmentCreateVM model,
            IFormFile? TitleImage,
            IFormFile? HODImage)
        {
            ViewData["Title"] = "Add Department";

            if (!ModelState.IsValid)
                return View(model);

            var dept = new Department
            {
                Name = model.Name,
                ShortCode = model.ShortCode,
                About = model.About,
                Intake = model.Intake,
                FacultyCount = model.FacultyCount,
                LabCount = model.LabCount,
                AnnualPlacement = model.AnnualPlacement,
                HODName = model.HODName,
                HODMessage = model.HODMessage,
                Tagline = model.Tagline,
                ShowIntake = model.ShowIntake,
                DisplayOrder = model.DisplayOrder,
                IsActive = true
            };

            // Handle title image upload
            if (TitleImage != null && TitleImage.Length > 0)
                dept.TitleImagePath = await SaveFileAsync(TitleImage, "departments");

            // Handle HOD image upload
            if (HODImage != null && HODImage.Length > 0)
                dept.HODImagePath = await SaveFileAsync(HODImage, "departments");

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Department";

            var dept = await _context.Departments
                .Include(d => d.Visions)
                .Include(d => d.Missions)
                .Include(d => d.PEOs)
                .Include(d => d.PSOs)
                .FirstOrDefaultAsync(d => d.DeptId == id);

            if (dept == null)
                return NotFound();

            // HOD restriction - can only edit own dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != id)
                    return Forbid();
            }

            var vm = new DepartmentEditVM
            {
                DeptId = dept.DeptId,
                Name = dept.Name,
                ShortCode = dept.ShortCode,
                About = dept.About,
                Intake = dept.Intake,
                FacultyCount = dept.FacultyCount,
                LabCount = dept.LabCount,
                AnnualPlacement = dept.AnnualPlacement,
                HODName = dept.HODName,
                HODMessage = dept.HODMessage,
                Tagline = dept.Tagline,
                ShowIntake = dept.ShowIntake,
                DisplayOrder = dept.DisplayOrder,
                ExistingTitleImagePath = dept.TitleImagePath,
                ExistingHODImagePath = dept.HODImagePath,
                VisionItems = dept.Visions
                    .OrderBy(v => v.DisplayOrder)
                    .Select(v => v.VisionText).ToList(),
                MissionItems = dept.Missions
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => m.MissionText).ToList(),
                PEOItems = dept.PEOs
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => p.PEOText).ToList(),
                PSOItems = dept.PSOs
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => p.PSOText).ToList()
            };

            return View(vm);
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            DepartmentEditVM model,
            IFormFile? TitleImage,
            IFormFile? HODImage,
            string? VisionItems,
            string? MissionItems,
            string? PEOItems,
            string? PSOItems)
        {
            ViewData["Title"] = "Edit Department";

            if (id != model.DeptId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            // HOD restriction
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != id)
                    return Forbid();
            }

            var dept = await _context.Departments
                .Include(d => d.Visions)
                .Include(d => d.Missions)
                .Include(d => d.PEOs)
                .Include(d => d.PSOs)
                .FirstOrDefaultAsync(d => d.DeptId == id);

            if (dept == null)
                return NotFound();

            // Update basic fields
            dept.Name = model.Name;
            dept.ShortCode = model.ShortCode;
            dept.About = model.About;
            dept.Intake = model.Intake;
            dept.FacultyCount = model.FacultyCount;
            dept.LabCount = model.LabCount;
            dept.AnnualPlacement = model.AnnualPlacement;
            dept.HODName = model.HODName;
            dept.HODMessage = model.HODMessage;
            dept.Tagline = model.Tagline;
            dept.ShowIntake = model.ShowIntake;
            dept.DisplayOrder = model.DisplayOrder;

            // Handle image uploads
            if (TitleImage != null && TitleImage.Length > 0)
            {
                DeleteFile(dept.TitleImagePath);
                dept.TitleImagePath = await SaveFileAsync(TitleImage, "departments");
            }

            if (HODImage != null && HODImage.Length > 0)
            {
                DeleteFile(dept.HODImagePath);
                dept.HODImagePath = await SaveFileAsync(HODImage, "departments");
            }

            // Update Vision items
            _context.DepartmentVisions.RemoveRange(dept.Visions);
            if (!string.IsNullOrEmpty(VisionItems))
            {
                var visions = VisionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < visions.Length; i++)
                {
                    var text = visions[i].Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _context.DepartmentVisions.Add(new DepartmentVision
                        {
                            DeptId = id,
                            VisionText = text,
                            DisplayOrder = i
                        });
                    }
                }
            }

            // Update Mission items
            _context.DepartmentMissions.RemoveRange(dept.Missions);
            if (!string.IsNullOrEmpty(MissionItems))
            {
                var missions = MissionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < missions.Length; i++)
                {
                    var text = missions[i].Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _context.DepartmentMissions.Add(new DepartmentMission
                        {
                            DeptId = id,
                            MissionText = text,
                            DisplayOrder = i
                        });
                    }
                }
            }

            // Update PEO items
            _context.DepartmentPEOs.RemoveRange(dept.PEOs);
            if (!string.IsNullOrEmpty(PEOItems))
            {
                var peos = PEOItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < peos.Length; i++)
                {
                    var text = peos[i].Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _context.DepartmentPEOs.Add(new DepartmentPEO
                        {
                            DeptId = id,
                            PEOText = text,
                            DisplayOrder = i
                        });
                    }
                }
            }

            // Update PSO items
            _context.DepartmentPSOs.RemoveRange(dept.PSOs);
            if (!string.IsNullOrEmpty(PSOItems))
            {
                var psos = PSOItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < psos.Length; i++)
                {
                    var text = psos[i].Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _context.DepartmentPSOs.Add(new DepartmentPSO
                        {
                            DeptId = id,
                            PSOText = text,
                            DisplayOrder = i
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
                return NotFound();

            dept.IsActive = !dept.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.Name}' " +
                (dept.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE (SOFT) ─────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
                return NotFound();

            dept.IsDeleted = true;
            dept.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Department '{dept.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null)
                return NotFound();

            if (direction == "up" && dept.DisplayOrder > 0)
            {
                var above = await _context.Departments
                    .Where(d => d.DisplayOrder == dept.DisplayOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null)
                {
                    above.DisplayOrder++;
                    dept.DisplayOrder--;
                }
            }
            else if (direction == "down")
            {
                var below = await _context.Departments
                    .Where(d => d.DisplayOrder == dept.DisplayOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null)
                {
                    below.DisplayOrder--;
                    dept.DisplayOrder++;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(
                _env.WebRootPath, "uploads", folder);

            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString()
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
                _env.WebRootPath,
                filePath.TrimStart('/'));

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null) return null;

            return await _context.Users.FindAsync(userId);
        }
    }
}