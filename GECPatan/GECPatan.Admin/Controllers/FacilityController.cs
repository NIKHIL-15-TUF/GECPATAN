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
        private const string FacilitiesFolder = "facilities";
        private const string SuperAdminRole = "SuperAdmin";
        private const int MaxBannerUploadsPerRequest = 10;

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<FacilityController> _logger;

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

            // Member count is computed in SQL — no need to load every member row.
            var list = await _context.Facilities
                .AsNoTracking()
                .OrderBy(f => f.DisplayOrder)
                .Select(f => new FacilityListVM
                {
                    Id = f.Id,
                    Title = f.Title,
                    Tagline = f.Tagline,
                    TitleImagePath = f.TitleImagePath,
                    IsActive = f.IsActive,
                    BlogspotLink = f.BlogspotLink,
                    DisplayOrder = f.DisplayOrder,
                    MemberCount = f.Members.Count(m => !m.IsDeleted)
                })
                .ToListAsync();

            var facilityIds = list.Select(f => f.Id).ToList();
            var sectionCounts = await _context.DynamicSections
                .Where(s => s.PageType == PageType.Facility && facilityIds.Contains(s.PageId))
                .GroupBy(s => s.PageId)
                .Select(g => new { PageId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PageId, x => x.Count);

            foreach (var item in list)
                item.SectionCount = sectionCounts.GetValueOrDefault(item.Id);

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        [Authorize(Roles = SuperAdminRole)]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Facility";
            return View(new FacilityCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
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

            try
            {
                // New facilities always go to the end; use Reorder to move them.
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

                _context.Facilities.Add(facility);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Facility {FacilityId} '{Title}' created by {User}",
                    facility.Id, facility.Title, User.Identity?.Name);

                TempData["Success"] = $"Facility '{facility.Title}' created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                TryDeleteFile(titleImagePath, "orphan cleanup");
                _logger.LogError(ex, "Database error creating facility '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to create the facility. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                TryDeleteFile(titleImagePath, "orphan cleanup");
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
                .AsNoTracking()
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return NotFound();

            var vm = new FacilityEditVM
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                About = f.About,
                BlogspotLink = f.BlogspotLink,
                VisionItems = f.Visions.OrderBy(v => v.DisplayOrder).Select(v => v.VisionText).ToList(),
                MissionItems = f.Missions.OrderBy(m => m.DisplayOrder).Select(m => m.MissionText).ToList()
            };

            await PopulateEditVmAsync(vm, id);
            return View(vm);
        }

        // VisionText / MissionText are the textarea field names. They are deliberately
        // different from the VM's VisionItems / MissionItems lists so the same form
        // field is never bound twice.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacilityEditVM model,
            IFormFile? TitleImage, List<IFormFile>? BannerImages,
            string? VisionText, string? MissionText)
        {
            ViewData["Title"] = "Edit Facility";
            if (id != model.Id) return NotFound();

            var bannerUploads = BannerImages?.Where(i => i.Length > 0).ToList() ?? new List<IFormFile>();
            if (bannerUploads.Count > MaxBannerUploadsPerRequest)
                ModelState.AddModelError(nameof(BannerImages),
                    $"You can upload at most {MaxBannerUploadsPerRequest} banner images at a time.");

            if (!ModelState.IsValid)
                return await RerenderEditAsync(model, id, VisionText, MissionText);

            var f = await _context.Facilities
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.BannerImages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return NotFound();

            // Every file written during this request. If the save fails, all of
            // them are removed so no upload is left on disk without a DB row.
            var newlySavedFiles = new List<string>();
            var skippedBanners = new List<string>();
            string? previousTitleImagePath = null;

            if (TitleImage != null && TitleImage.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(TitleImage, FacilitiesFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), uploadResult.ErrorMessage!);
                    return await RerenderEditAsync(model, id, VisionText, MissionText);
                }
                newlySavedFiles.Add(uploadResult.RelativePath!);
                previousTitleImagePath = f.TitleImagePath;
                f.TitleImagePath = uploadResult.RelativePath;
            }

            if (bannerUploads.Count > 0)
            {
                int order = f.BannerImages.Any() ? f.BannerImages.Max(b => b.DisplayOrder) + 1 : 0;

                foreach (var img in bannerUploads)
                {
                    var uploadResult = await _fileStorage.SaveAsync(img, FacilitiesFolder, FileCategory.Image);
                    if (!uploadResult.Success)
                    {
                        // One bad file shouldn't discard the whole edit — skip it and tell the admin.
                        skippedBanners.Add($"{img.FileName}: {uploadResult.ErrorMessage}");
                        continue;
                    }

                    newlySavedFiles.Add(uploadResult.RelativePath!);
                    f.BannerImages.Add(new FacilityBannerImage
                    {
                        FacilityId = id,
                        ImagePath = uploadResult.RelativePath!,
                        DisplayOrder = order++
                    });
                }
            }

            // DisplayOrder is intentionally not edited here — Reorder owns it.
            f.Title = model.Title;
            f.Tagline = model.Tagline;
            f.About = model.About;
            f.BlogspotLink = model.BlogspotLink;

            ReplaceTextList(_context.FacilityVisions, f.Visions, VisionText,
                (text, order) => new FacilityVision { FacilityId = id, VisionText = text, DisplayOrder = order });

            ReplaceTextList(_context.FacilityMissions, f.Missions, MissionText,
                (text, order) => new FacilityMission { FacilityId = id, MissionText = text, DisplayOrder = order });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating facility {FacilityId}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Unable to save the facility. Please try again.");
                return await RerenderEditAsync(model, id, VisionText, MissionText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating facility {FacilityId}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return await RerenderEditAsync(model, id, VisionText, MissionText);
            }

            // Old title image is removed only after the new state is safely persisted.
            TryDeleteFile(previousTitleImagePath, "replaced title image");

            _logger.LogInformation("Facility {FacilityId} '{Title}' updated by {User}",
                f.Id, f.Title, User.Identity?.Name);

            TempData["Success"] = $"Facility '{f.Title}' updated.";
            if (skippedBanners.Any())
                TempData["Warning"] = "Some banner images were skipped: " + string.Join(" | ", skippedBanners);

            return RedirectToAction(nameof(Index));
        }

        // ── DELETE BANNER IMAGE (AJAX) ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBannerImage(int id, int facilityId)
        {
            var img = await _context.Set<FacilityBannerImage>()
                .FirstOrDefaultAsync(b => b.Id == id && b.FacilityId == facilityId);
            if (img == null) return NotFound();

            var imagePath = img.ImagePath;

            try
            {
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete banner image {ImageId} for facility {FacilityId}", id, facilityId);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            // File is removed only after the DB change is committed.
            TryDeleteFile(imagePath, "deleted banner image");
            return Ok();
        }

        // ── TOGGLE / DELETE ───────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
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

        // Soft delete. Files are intentionally kept on disk so the facility can be
        // restored with its images intact.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();

            f.IsDeleted = true;
            f.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Facility {FacilityId} '{Title}' deleted by {User}",
                    id, f.Title, User.Identity?.Name);
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
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            if (direction != "up" && direction != "down")
                return RedirectToAction(nameof(Index));

            // Swaps with the nearest neighbour, so gaps left by deletes don't
            // make the arrows silently do nothing. The transaction prevents a
            // half-swapped state. Two admins reordering at the exact same moment
            // can still race (no concurrency token on the table).
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var f = await _context.Facilities.FindAsync(id);
                if (f == null) return NotFound();

                Facility? neighbour = direction == "up"
                    ? await _context.Facilities
                        .Where(x => x.DisplayOrder < f.DisplayOrder)
                        .OrderByDescending(x => x.DisplayOrder)
                        .FirstOrDefaultAsync()
                    : await _context.Facilities
                        .Where(x => x.DisplayOrder > f.DisplayOrder)
                        .OrderBy(x => x.DisplayOrder)
                        .FirstOrDefaultAsync();

                if (neighbour != null)
                    (neighbour.DisplayOrder, f.DisplayOrder) = (f.DisplayOrder, neighbour.DisplayOrder);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to reorder facility {FacilityId}", id);
                TempData["Error"] = "Unable to reorder facilities. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Facility Members";
            var facility = await _context.Facilities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (facility == null) return NotFound();

            ViewBag.FacilityId = id;
            ViewBag.FacilityTitle = facility.Title;

            var members = await _context.FacilityMembers
                .AsNoTracking()
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

            if (!await _context.Facilities.AnyAsync(f => f.Id == model.FacilityId))
                return NotFound();

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
            catch (Exception ex)
            {
                TryDeleteFile(imagePath, "orphan cleanup");
                _logger.LogError(ex, "Error adding member to facility {FacilityId}", model.FacilityId);
                TempData["Error"] = "Unable to add the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = model.FacilityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int facilityId)
        {
            // The member must belong to the facility named in the request.
            var m = await _context.FacilityMembers
                .FirstOrDefaultAsync(x => x.Id == id && x.FacilityId == facilityId);
            if (m == null) return NotFound();

            m.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
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

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// Fills the read-only / display parts of the edit VM that are never posted
        /// back (current images, banners, counts). Uses fresh no-tracking queries so
        /// it is also safe after a failed SaveChanges.
        /// </summary>
        private async Task PopulateEditVmAsync(FacilityEditVM model, int id)
        {
            model.ExistingTitleImagePath = await _context.Facilities
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.TitleImagePath)
                .FirstOrDefaultAsync();

            model.ExistingBannerImages = await _context.Set<FacilityBannerImage>()
                .AsNoTracking()
                .Where(b => b.FacilityId == id && !b.IsDeleted)
                .OrderBy(b => b.DisplayOrder)
                .Select(b => new FacilityBannerImageVM { Id = b.Id, ImagePath = b.ImagePath })
                .ToListAsync();

            model.MemberCount = await _context.FacilityMembers
                .CountAsync(m => m.FacilityId == id && !m.IsDeleted);

            model.SectionCount = await _context.DynamicSections
                .CountAsync(s => s.PageType == PageType.Facility && s.PageId == id);
        }

        /// <summary>
        /// Re-displays the edit form after a failure without losing what the admin
        /// typed (vision/mission text) or the page's read-only data.
        /// </summary>
        private async Task<IActionResult> RerenderEditAsync(
            FacilityEditVM model, int id, string? visionText, string? missionText)
        {
            model.VisionItems = SplitLines(visionText);
            model.MissionItems = SplitLines(missionText);
            await PopulateEditVmAsync(model, id);
            return View(nameof(Edit), model);
        }

        private static List<string> SplitLines(string? raw) =>
            string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

        /// <summary>
        /// Replaces a child text list (vision / mission) with the newline-separated
        /// values submitted by the form.
        /// </summary>
        private static void ReplaceTextList<T>(
            DbSet<T> set,
            IEnumerable<T> existing,
            string? rawItems,
            Func<string, int, T> factory) where T : class
        {
            set.RemoveRange(existing.ToList());

            int order = 0;
            foreach (var text in SplitLines(rawItems))
                set.Add(factory(text, order++));
        }

        /// <summary>
        /// Deletes a file and never throws. A failed delete is logged so it can be
        /// cleaned up manually, but it must never take down a request or mask the
        /// original error inside a catch block.
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

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDeleteFile(path, "orphan cleanup");
        }
    }
}