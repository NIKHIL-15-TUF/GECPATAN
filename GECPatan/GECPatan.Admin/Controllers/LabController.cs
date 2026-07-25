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
    public class LabController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<LabController> _logger;
        private const string LabsFolder = "labs";

        public LabController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<LabController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
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
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != null)
                    query = query.Where(l => l.DeptId == currentUser.DeptId);
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

            var currentUser = await GetCurrentUserAsync();
            var vm = new LabCreateVM
            {
                DeptId = User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != null
                    ? currentUser.DeptId.Value
                    : deptId ?? 0
            };
            return View(await BuildCreateVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LabCreateVM model)
        {
            ViewData["Title"] = "Add Lab";

            var currentUser = await GetCurrentUserAsync();

            // HOD can only create labs for their own department
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != null)
                model.DeptId = currentUser.DeptId.Value;

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            var imageFiles = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();

            // Validate & save all images before touching the database.
            var savedPaths = new List<string>();
            foreach (var file in imageFiles)
            {
                var result = await _fileStorage.SaveAsync(file, LabsFolder, FileCategory.Image);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(string.Empty, $"'{file.FileName}': {result.ErrorMessage}");
                    return View(await BuildCreateVM(model));
                }
                savedPaths.Add(result.RelativePath!);
            }

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

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Labs.Add(lab);
                await _context.SaveChangesAsync();

                int order = 0;
                foreach (var path in savedPaths)
                {
                    _context.LabImages.Add(new LabImage
                    {
                        LabId = lab.LabId,
                        ImagePath = path,
                        DisplayOrder = order++
                    });
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Lab {LabId} '{LabName}' created in dept {DeptId} with {ImageCount} image(s)",
                    lab.LabId, lab.LabName, lab.DeptId, savedPaths.Count);

                TempData["Success"] = $"Lab '{lab.LabName}' added.";
                return RedirectToAction(nameof(Index), new { deptId = model.DeptId });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error creating lab '{LabName}' in dept {DeptId}", model.LabName, model.DeptId);
                ModelState.AddModelError(string.Empty, "Unable to save the lab. Please try again.");
                return View(await BuildCreateVM(model));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error creating lab '{LabName}'", model.LabName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(await BuildCreateVM(model));
            }
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Lab";
            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            var vm = new LabEditVM
            {
                LabId = lab.LabId,
                LabName = lab.LabName,
                About = lab.About,
                DeptId = lab.DeptId,
                DisplayOrder = lab.DisplayOrder,
                IsVisible = lab.IsVisible,
                ExistingImagePaths = lab.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .ToList()
            };

            ViewBag.ExistingImages = lab.Images
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.DisplayOrder).ToList();

            return View(await BuildEditVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LabEditVM model)
        {
            ViewData["Title"] = "Edit Lab";

            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            // HOD can't move a lab to a different department
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != null)
                model.DeptId = currentUser.DeptId.Value;

            if (!await _context.Departments.AnyAsync(d => d.DeptId == model.DeptId))
                ModelState.AddModelError(nameof(model.DeptId), "Please select a valid department.");

            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var imageFiles = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();

            var savedPaths = new List<string>();
            foreach (var file in imageFiles)
            {
                var result = await _fileStorage.SaveAsync(file, LabsFolder, FileCategory.Image);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(string.Empty, $"'{file.FileName}': {result.ErrorMessage}");
                    return View(await BuildEditVM(model));
                }
                savedPaths.Add(result.RelativePath!);
            }

            lab.LabName = model.LabName;
            lab.About = model.About;
            lab.DeptId = model.DeptId;
            lab.DisplayOrder = model.DisplayOrder;
            lab.IsVisible = model.IsVisible;

            int order = lab.Images.Any(i => !i.IsDeleted)
                ? lab.Images.Where(i => !i.IsDeleted).Max(i => i.DisplayOrder) + 1 : 0;

            try
            {
                foreach (var path in savedPaths)
                {
                    _context.LabImages.Add(new LabImage
                    {
                        LabId = id,
                        ImagePath = path,
                        DisplayOrder = order++
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Lab {LabId} updated with {NewImageCount} new image(s)", id, savedPaths.Count);

                TempData["Success"] = $"Lab '{lab.LabName}' updated.";
                return RedirectToAction(nameof(Index), new { deptId = lab.DeptId });
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error updating lab {LabId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the lab. Please try again.");
                return View(await BuildEditVM(model));
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error updating lab {LabId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(await BuildEditVM(model));
            }
        }

        // ── DELETE IMAGE ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int labId)
        {
            var img = await _context.LabImages.FindAsync(id);
            if (img == null) return NotFound();

            var lab = await _context.Labs.FindAsync(labId);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            string? imagePath = img.ImagePath;
            img.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Image {ImageId} removed from lab {LabId}", id, labId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error removing image {ImageId} from lab {LabId}", id, labId);
                TempData["Error"] = "Unable to remove the image. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = labId });
        }

        // ── TOGGLE / DELETE ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var lab = await _context.Labs.FindAsync(id);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            lab.IsVisible = !lab.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Lab {LabId} visibility set to {IsVisible}", id, lab.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for lab {LabId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lab = await _context.Labs
                .Include(l => l.Images)
                .FirstOrDefaultAsync(l => l.LabId == id);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            var imagePaths = lab.Images.Select(i => i.ImagePath).ToList();

            lab.IsDeleted = true;
            foreach (var img in lab.Images)
                img.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                foreach (var path in imagePaths)
                    _fileStorage.Delete(path);

                _logger.LogInformation("Lab {LabId} '{LabName}' deleted with {ImageCount} image(s)",
                    id, lab.LabName, imagePaths.Count);

                TempData["Success"] = $"Lab '{lab.LabName}' deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting lab {LabId}", id);
                TempData["Error"] = "Unable to delete the lab. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var lab = await _context.Labs.FindAsync(id);
            if (lab == null) return NotFound();

            var currentUser = await GetCurrentUserAsync();
            if (User.IsInRole(AppRoles.HOD) && currentUser?.DeptId != lab.DeptId)
                return Forbid();

            if (direction == "up")
            {
                var above = await _context.Labs
                    .Where(l => l.DeptId == lab.DeptId && l.DisplayOrder == lab.DisplayOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; lab.DisplayOrder--; }
            }
            else if (direction == "down")
            {
                var below = await _context.Labs
                    .Where(l => l.DeptId == lab.DeptId && l.DisplayOrder == lab.DisplayOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; lab.DisplayOrder++; }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering lab {LabId}", id);
                TempData["Error"] = "Unable to reorder labs. Please try again.";
            }

            return RedirectToAction(nameof(Index), new { deptId = lab.DeptId });
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

        private Task<ApplicationUser?> GetCurrentUserAsync() =>
            _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
    }
}