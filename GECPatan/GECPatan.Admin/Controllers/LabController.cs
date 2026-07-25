using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor")]
    public class LabController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LabController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Labs";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var query = _context.Labs
                .Include(l => l.Department)
                .Include(l => l.Images)
                .AsQueryable();

            // HOD sees only their dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var cu = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (cu?.DeptId != null)
                    query = query.Where(l => l.DeptId == cu.DeptId);
            }
            else if (deptId.HasValue)
            {
                query = query.Where(l => l.DeptId == deptId.Value);
            }

            var list = await query
                .OrderBy(l => l.DisplayOrder)
                .Select(l => new LabListVM
                {
                    LabId = l.LabId,
                    LabName = l.LabName,
                    DeptName = l.Department != null ? l.Department.Name : "",
                    ImageCount = l.Images.Count,
                    IsVisible = l.IsVisible,
                    DisplayOrder = l.DisplayOrder
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Add Lab";
            var vm = new LabCreateVM { DeptId = deptId ?? 0 };
            return View(await BuildCreateVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LabCreateVM model)
        {
            ViewData["Title"] = "Add Lab";
            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            int maxOrder = await _context.Labs
                .Where(l => l.DeptId == model.DeptId)
                .Select(l => (int?)l.DisplayOrder).MaxAsync() ?? -1;

            var lab = new Lab
            {
                LabName = model.LabName,
                About = model.About,
                DeptId = model.DeptId,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            _context.Labs.Add(lab);
            await _context.SaveChangesAsync();

            // Save images
            int order = 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
                if (file.Length > 0)
                    _context.LabImages.Add(new LabImage
                    {
                        LabId = lab.LabId,
                        ImagePath = await SaveFileAsync(file, "labs"),
                        DisplayOrder = order++
                    });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Lab '{lab.LabName}' added.";
            return RedirectToAction(nameof(Index),
                new { deptId = model.DeptId });
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Lab";
            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            var vm = new LabEditVM
            {
                LabId = lab.LabId,
                LabName = lab.LabName,
                About = lab.About,
                DeptId = lab.DeptId,
                DisplayOrder = lab.DisplayOrder,
                IsVisible = lab.IsVisible,
                ExistingImagePaths = lab.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .ToList()
            };

            ViewBag.ExistingImages = lab.Images
                .OrderBy(i => i.DisplayOrder).ToList();

            return View(await BuildEditVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LabEditVM model)
        {
            ViewData["Title"] = "Edit Lab";
            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            lab.LabName = model.LabName;
            lab.About = model.About;
            lab.DeptId = model.DeptId;
            lab.DisplayOrder = model.DisplayOrder;
            lab.IsVisible = model.IsVisible;

            // Add new images
            int order = lab.Images.Any()
                ? lab.Images.Max(i => i.DisplayOrder) + 1 : 0;

            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
                if (file.Length > 0)
                    _context.LabImages.Add(new LabImage
                    {
                        LabId = id,
                        ImagePath = await SaveFileAsync(file, "labs"),
                        DisplayOrder = order++
                    });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Lab '{lab.LabName}' updated.";
            return RedirectToAction(nameof(Index),
                new { deptId = lab.DeptId });
        }

        // ── DELETE IMAGE ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, int labId)
        {
            var img = await _context.LabImages.FindAsync(id);
            if (img != null)
            {
                DeleteFile(img.ImagePath);
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Edit), new { id = labId });
        }

        // ── TOGGLE / DELETE ───────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var lab = await _context.Labs.FindAsync(id);
            if (lab == null) return NotFound();
            lab.IsVisible = !lab.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            foreach (var img in lab.Images)
                DeleteFile(img.ImagePath);

            lab.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Lab '{lab.LabName}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var lab = await _context.Labs.FindAsync(id);
            if (lab == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.Labs
                    .Where(l => l.DeptId == lab.DeptId &&
                                l.DisplayOrder == lab.DisplayOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; lab.DisplayOrder--; }
            }
            else
            {
                var below = await _context.Labs
                    .Where(l => l.DeptId == lab.DeptId &&
                                l.DisplayOrder == lab.DisplayOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; lab.DisplayOrder++; }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index),
                new { deptId = lab.DeptId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<LabCreateVM> BuildCreateVM(LabCreateVM vm)
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

        private async Task<LabEditVM> BuildEditVM(LabEditVM vm)
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
    }
}