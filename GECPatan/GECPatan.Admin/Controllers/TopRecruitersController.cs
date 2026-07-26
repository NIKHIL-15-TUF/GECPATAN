using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,PlacementOfficer")]
    public class TopRecruiterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<TopRecruiterController> _logger;
        private const string RecruitersFolder = "recruiters";

        public TopRecruiterController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<TopRecruiterController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Top Recruiters";
            var items = await _context.TopRecruiters
                .OrderBy(r => r.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Recruiter";
            return View(new TopRecruiterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TopRecruiterVM model, IFormFile? Logo)
        {
            ViewData["Title"] = "Add Recruiter";
            if (!ModelState.IsValid) return View(model);

            string? logoPath = null;
            if (Logo != null && Logo.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Logo, RecruitersFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Logo), uploadResult.ErrorMessage!);
                    return View(model);
                }
                logoPath = uploadResult.RelativePath;
            }

            int maxOrder = await _context.TopRecruiters.Select(r => (int?)r.DisplayOrder).MaxAsync() ?? -1;

            var recruiter = new TopRecruiter
            {
                Name = model.Name,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible,
                LogoPath = logoPath
            };

            try
            {
                _context.TopRecruiters.Add(recruiter);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recruiter {RecruiterId} '{Name}' created", recruiter.Id, recruiter.Name);

                TempData["Success"] = "Recruiter added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(logoPath);
                _logger.LogError(ex, "Database error creating recruiter '{Name}'", model.Name);
                ModelState.AddModelError(string.Empty, "Unable to save the recruiter. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(logoPath);
                _logger.LogError(ex, "Unexpected error creating recruiter '{Name}'", model.Name);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Recruiter";
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            return View(new TopRecruiterVM
            {
                Id = r.Id,
                Name = r.Name,
                DisplayOrder = r.DisplayOrder,
                IsVisible = r.IsVisible,
                ExistingLogoPath = r.LogoPath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TopRecruiterVM model, IFormFile? Logo)
        {
            ViewData["Title"] = "Edit Recruiter";
            if (!ModelState.IsValid) return View(model);

            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            string? newLogoPath = null;
            bool replacingLogo = Logo != null && Logo.Length > 0;
            if (replacingLogo)
            {
                var uploadResult = await _fileStorage.SaveAsync(Logo!, RecruitersFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Logo), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newLogoPath = uploadResult.RelativePath;
            }

            string? previousLogoPath = r.LogoPath;

            r.Name = model.Name;
            r.IsVisible = model.IsVisible;

            if (replacingLogo)
                r.LogoPath = newLogoPath;

            try
            {
                await _context.SaveChangesAsync();

                // Old logo removed only after the new state is safely persisted.
                if (replacingLogo)
                    _fileStorage.Delete(previousLogoPath);

                _logger.LogInformation("Recruiter {RecruiterId} '{Name}' updated", r.Id, r.Name);

                TempData["Success"] = "Recruiter updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingLogo)
                    _fileStorage.Delete(newLogoPath);

                _logger.LogError(ex, "Database error updating recruiter {RecruiterId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the recruiter. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingLogo)
                    _fileStorage.Delete(newLogoPath);

                _logger.LogError(ex, "Unexpected error updating recruiter {RecruiterId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            r.IsVisible = !r.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Recruiter {RecruiterId} visibility set to {IsVisible}", id, r.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for recruiter {RecruiterId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            string? logoPath = r.LogoPath;
            r.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Physical file removed only after the soft-delete commits.
                _fileStorage.Delete(logoPath);

                _logger.LogInformation("Recruiter {RecruiterId} '{Name}' deleted", id, r.Name);
                TempData["Success"] = "Recruiter deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting recruiter {RecruiterId}", id);
                TempData["Error"] = "Unable to delete the recruiter. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.TopRecruiters.Where(x => x.DisplayOrder == r.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; r.DisplayOrder--; }
            }
            else if (direction == "down")
            {
                var below = await _context.TopRecruiters.Where(x => x.DisplayOrder == r.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; r.DisplayOrder++; }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering recruiter {RecruiterId}", id);
                TempData["Error"] = "Unable to reorder recruiters. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}