using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class FacilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<FacilityController> _logger;
        private const string FacilitiesFolder = "facilities";

        public FacilityController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<FacilityController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Facilities";
            var facilities = await _context.Facilities
                .Include(f => f.Members)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            var facilityIds = facilities.Select(f => f.Id).ToList();
            var sectionCounts = await _context.DynamicSections
                .Where(s => s.PageType == PageType.Facility && facilityIds.Contains(s.PageId))
                .GroupBy(s => s.PageId)
                .Select(g => new { PageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PageId, x => x.Count);

            var list = facilities.Select(f => new FacilityListVM
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                TitleImagePath = f.TitleImagePath,
                IsActive = f.IsActive,
                BlogspotLink = f.BlogspotLink,
                DisplayOrder = f.DisplayOrder,
                MemberCount = f.Members.Count,
                SectionCount = sectionCounts.GetValueOrDefault(f.Id)
            }).ToList();

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Facility";
            return View(new FacilityCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(FacilityCreateVM model, IFormFile? TitleImage)
        {
            ViewData["Title"] = "Add Facility";
            if (!ModelState.IsValid) return View(model);

            string? titleImagePath = null;
            if (TitleImage != null && TitleImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleImage, FacilitiesFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                titleImagePath = uploadResult.RelativePath;
            }

            int maxOrder = await _context.Facilities
                .Select(f => (int?)f.DisplayOrder).MaxAsync() ?? -1;

            var facility = new Facility
            {
                Title = model.Title,
                Tagline = model.Tagline,
                About = model.About,
                BlogspotLink = model.BlogspotLink,
                DisplayOrder = maxOrder + 1,
                IsActive = true,
                TitleImagePath = titleImagePath
            };

            try
            {
                _context.Facilities.Add(facility);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Facility {FacilityId} '{Title}' created", facility.Id, facility.Title);

                TempData["Success"] = $"Facility '{facility.Title}' created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(titleImagePath);
                _logger.LogError(ex, "Database error creating facility '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to create the facility. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(titleImagePath);
                _logger.LogError(ex, "Unexpected error creating facility '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Facility";
            var f = await _context.Facilities
                .Include(x => x.Members)
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return NotFound();

            var sectionCount = await _context.DynamicSections
                .CountAsync(s => s.PageType == PageType.Facility && s.PageId == id);

            return View(new FacilityEditVM
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                About = f.About,
                DisplayOrder = f.DisplayOrder,
                BlogspotLink = f.BlogspotLink,
                ExistingTitleImagePath = f.TitleImagePath,
                MemberCount = f.Members.Count,
                SectionCount = sectionCount,
                VisionItems = f.Visions.OrderBy(v => v.DisplayOrder).Select(v => v.VisionText).ToList(),
                MissionItems = f.Missions.OrderBy(m => m.DisplayOrder).Select(m => m.MissionText).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacilityEditVM model,
            IFormFile? TitleImage, string? VisionItems, string? MissionItems)
        {
            ViewData["Title"] = "Edit Facility";
            if (!ModelState.IsValid) return View(model);

            var f = await _context.Facilities
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return NotFound();

            string? newTitleImagePath = null;
            bool replacingImage = TitleImage != null && TitleImage.Length > 0;
            if (replacingImage)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleImage!, FacilitiesFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), uploadResult.ErrorMessage!);
                    return View(model);
                }
                newTitleImagePath = uploadResult.RelativePath;
            }

            string? previousTitleImagePath = f.TitleImagePath;

            f.Title = model.Title;
            f.Tagline = model.Tagline;
            f.About = model.About;
            f.BlogspotLink = model.BlogspotLink;
            f.DisplayOrder = model.DisplayOrder;

            if (replacingImage)
                f.TitleImagePath = newTitleImagePath;

            _context.FacilityVisions.RemoveRange(f.Visions);
            if (!string.IsNullOrEmpty(VisionItems))
            {
                int i = 0;
                foreach (var line in VisionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(line))
                        _context.FacilityVisions.Add(new FacilityVision
                        {
                            FacilityId = id,
                            VisionText = line.Trim(),
                            DisplayOrder = i++
                        });
            }

            _context.FacilityMissions.RemoveRange(f.Missions);
            if (!string.IsNullOrEmpty(MissionItems))
            {
                int i = 0;
                foreach (var line in MissionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(line))
                        _context.FacilityMissions.Add(new FacilityMission
                        {
                            FacilityId = id,
                            MissionText = line.Trim(),
                            DisplayOrder = i++
                        });
            }

            try
            {
                await _context.SaveChangesAsync();

                // Old image is only removed once the new state is safely persisted.
                if (replacingImage)
                    _fileStorage.Delete(previousTitleImagePath);

                _logger.LogInformation("Facility {FacilityId} '{Title}' updated", f.Id, f.Title);

                TempData["Success"] = $"Facility '{f.Title}' updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingImage)
                    _fileStorage.Delete(newTitleImagePath);

                _logger.LogError(ex, "Database error updating facility {FacilityId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the facility. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                if (replacingImage)
                    _fileStorage.Delete(newTitleImagePath);

                _logger.LogError(ex, "Unexpected error updating facility {FacilityId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // ── TOGGLE / DELETE ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();
            f.IsActive = !f.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Facility {FacilityId} active state set to {IsActive}", id, f.IsActive);
                TempData["Success"] = $"'{f.Title}' " + (f.IsActive ? "activated" : "deactivated") + ".";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling facility {FacilityId}", id);
                TempData["Error"] = "Unable to update the facility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();

            string? imagePath = f.TitleImagePath;
            f.IsDeleted = true;
            f.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();

                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Facility {FacilityId} '{Title}' deleted", id, f.Title);
                TempData["Success"] = $"Facility '{f.Title}' deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting facility {FacilityId}", id);
                TempData["Error"] = "Unable to delete the facility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.Facilities
                    .Where(x => x.DisplayOrder == f.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; f.DisplayOrder--; }
            }
            else if (direction == "down")
            {
                var below = await _context.Facilities
                    .Where(x => x.DisplayOrder == f.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; f.DisplayOrder++; }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error reordering facility {FacilityId}", id);
                TempData["Error"] = "Unable to reorder facilities. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Facility Members";
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null) return NotFound();

            ViewBag.FacilityId = id;
            ViewBag.FacilityTitle = facility.Title;

            var members = await _context.FacilityMembers
                .Where(m => m.FacilityId == id && !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(FacilityMemberVM model, IFormFile? Photo)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the errors and try again.";
                return RedirectToAction(nameof(Members), new { id = model.FacilityId });
            }

            string? imagePath = null;
            if (Photo != null && Photo.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Photo, FacilitiesFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    TempData["Error"] = uploadResult.ErrorMessage;
                    return RedirectToAction(nameof(Members), new { id = model.FacilityId });
                }
                imagePath = uploadResult.RelativePath;
            }

            var member = new FacilityMember
            {
                FacilityId = model.FacilityId,
                Name = model.Name,
                Position = model.Position,
                Department = model.Department,
                Email = model.Email,
                Contact = model.Contact,
                DisplayOrder = model.DisplayOrder,
                ImagePath = imagePath
            };

            try
            {
                _context.FacilityMembers.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Member {MemberId} '{Name}' added to facility {FacilityId}",
                    member.Id, member.Name, member.FacilityId);
                TempData["Success"] = "Member added.";
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(imagePath);
                _logger.LogError(ex, "Database error adding member to facility {FacilityId}", model.FacilityId);
                TempData["Error"] = "Unable to add the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = model.FacilityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int facilityId)
        {
            var m = await _context.FacilityMembers.FindAsync(id);
            if (m == null) return NotFound();

            string? imagePath = m.ImagePath;
            m.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Member {MemberId} deleted from facility {FacilityId}", id, facilityId);
                TempData["Success"] = "Member removed.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting member {MemberId}", id);
                TempData["Error"] = "Unable to remove the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = facilityId });
        }
    }
}