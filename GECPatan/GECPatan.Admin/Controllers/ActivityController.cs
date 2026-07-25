using System.Security.Claims;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class ActivityController : Controller
    {
        private const string UploadFolder = "activities";

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<ActivityController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId, int? committeeId)
        {
            ViewData["Title"] = "Activities";
            ViewBag.SelectedDeptId = deptId;
            ViewBag.SelectedCommitteeId = committeeId;
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).AsNoTracking().ToListAsync(), "DeptId", "Name", deptId);
            ViewBag.Committees = new SelectList(
                await _context.CampusCommittees.OrderBy(c => c.Title).AsNoTracking().ToListAsync(), "Id", "Title", committeeId);

            var query = _context.Activities.AsNoTracking().Where(a => !a.IsDeleted);
            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);
            if (committeeId.HasValue) query = query.Where(a => a.CommitteeId == committeeId);

            var list = await query
                .Include(a => a.Images)
                .Include(a => a.Files)
                .OrderByDescending(a => a.EventDate)
                .Select(a => new ActivityListVM
                {
                    Id = a.Id,
                    Title = a.Title,
                    EventDate = a.EventDate.HasValue ? a.EventDate.Value.ToString("dd MMM yyyy") : "",
                    IsVisible = a.IsVisible,
                    ImageCount = a.Images.Count(i => !i.IsDeleted),
                    FileCount = a.Files.Count(f => !f.IsDeleted)
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Activity";
            return View(await BuildCreateVM(new ActivityCreateVM()));
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateVM model)
        {
            ViewData["Title"] = "Add Activity";

            // HOD can only publish activities under their own department.
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            // Uploaded before the transaction starts, same as the other
            // controllers: validation failures shouldn't touch the database
            // at all, and successful uploads get tracked so they can be
            // cleaned up if the DB writes below fail.
            var imageOutcomes = await SaveFormFilesAsync("Images", FileCategory.Image);
            var fileOutcomes = await SaveFormFilesAsync("Files", FileCategory.Document);

            var newlySavedFiles = imageOutcomes.Concat(fileOutcomes)
                .Where(o => o.Result.Success)
                .Select(o => o.Result.RelativePath!)
                .ToList();
            var skipped = imageOutcomes.Concat(fileOutcomes)
                .Where(o => !o.Result.Success)
                .Select(o => $"{o.OriginalFileName}: {o.Result.ErrorMessage}")
                .ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var activity = new Activity
                {
                    Title = model.Title,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    EventTime = model.EventTime,
                    Year = model.Year ?? model.EventDate?.Year,
                    TargetStudents = model.TargetStudents,
                    Keywords = model.Keywords,
                    ExternalLink = model.ExternalLink,
                    DeptId = model.DeptId,
                    CommitteeId = model.CommitteeId,
                    ClubId = model.ClubId,
                    IsVisible = model.IsVisible,
                    IsFile = model.IsFile
                };

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                int imgOrder = 0;
                foreach (var outcome in imageOutcomes.Where(o => o.Result.Success))
                {
                    _context.ActivityImages.Add(new ActivityImage
                    {
                        ActivityId = activity.Id,
                        ImagePath = outcome.Result.RelativePath!,
                        DisplayOrder = imgOrder++
                    });
                }

                int fileOrder = 0;
                foreach (var outcome in fileOutcomes.Where(o => o.Result.Success))
                {
                    _context.ActivityFiles.Add(new ActivityFile
                    {
                        ActivityId = activity.Id,
                        FilePath = outcome.Result.RelativePath!,
                        Title = Path.GetFileNameWithoutExtension(outcome.OriginalFileName),
                        FileType = ExtensionLabel(outcome.OriginalFileName),
                        DisplayOrder = fileOrder++
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Activity {Id} '{Title}' created by {User}", activity.Id, activity.Title, User.Identity?.Name);
                TempData["Success"] = "Activity added successfully.";
                if (skipped.Any())
                    TempData["Warning"] = "Some files were skipped: " + string.Join(" | ", skipped);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error while creating activity '{Title}'", model.Title);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the activity due to a database error. Please try again.");
                return View(await BuildCreateVM(model));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error while creating activity '{Title}'", model.Title);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the activity. Please try again.");
                return View(await BuildCreateVM(model));
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Activity";

            var a = await _context.Activities
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            var vm = new ActivityEditVM
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                EventDate = a.EventDate,
                EventTime = a.EventTime,
                Year = a.Year,
                TargetStudents = a.TargetStudents,
                Keywords = a.Keywords,
                ExternalLink = a.ExternalLink,
                DeptId = a.DeptId,
                CommitteeId = a.CommitteeId,
                ClubId = a.ClubId,
                IsVisible = a.IsVisible,
                IsFile = a.IsFile
            };

            ViewBag.ExistingImages = a.Images.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).ToList();
            ViewBag.ExistingFiles = a.Files.Where(f => !f.IsDeleted).OrderBy(f => f.DisplayOrder).ToList();

            return View(await BuildEditVM(vm));
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActivityEditVM model)
        {
            ViewData["Title"] = "Edit Activity";

            var a = await _context.Activities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId || model.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var imageOutcomes = await SaveFormFilesAsync("Images", FileCategory.Image);
            var fileOutcomes = await SaveFormFilesAsync("Files", FileCategory.Document);

            var newlySavedFiles = imageOutcomes.Concat(fileOutcomes)
                .Where(o => o.Result.Success)
                .Select(o => o.Result.RelativePath!)
                .ToList();
            var skipped = imageOutcomes.Concat(fileOutcomes)
                .Where(o => !o.Result.Success)
                .Select(o => $"{o.OriginalFileName}: {o.Result.ErrorMessage}")
                .ToList();

            a.Title = model.Title;
            a.Description = model.Description;
            a.EventDate = model.EventDate;
            a.EventTime = model.EventTime;
            a.Year = model.Year ?? model.EventDate?.Year;
            a.TargetStudents = model.TargetStudents;
            a.Keywords = model.Keywords;
            a.ExternalLink = model.ExternalLink;
            a.DeptId = model.DeptId;
            a.CommitteeId = model.CommitteeId;
            a.ClubId = model.ClubId;
            a.IsVisible = model.IsVisible;
            a.IsFile = model.IsFile;

            int imgOrder = await _context.ActivityImages.Where(i => i.ActivityId == id && !i.IsDeleted).CountAsync();
            foreach (var outcome in imageOutcomes.Where(o => o.Result.Success))
            {
                _context.ActivityImages.Add(new ActivityImage
                {
                    ActivityId = id,
                    ImagePath = outcome.Result.RelativePath!,
                    DisplayOrder = imgOrder++
                });
            }

            int fileOrder = await _context.ActivityFiles.Where(f => f.ActivityId == id && !f.IsDeleted).CountAsync();
            foreach (var outcome in fileOutcomes.Where(o => o.Result.Success))
            {
                _context.ActivityFiles.Add(new ActivityFile
                {
                    ActivityId = id,
                    FilePath = outcome.Result.RelativePath!,
                    Title = Path.GetFileNameWithoutExtension(outcome.OriginalFileName),
                    FileType = ExtensionLabel(outcome.OriginalFileName),
                    DisplayOrder = fileOrder++
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing activity {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the activity due to a database error. Please try again.");
                return View(await BuildEditVM(model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing activity {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the activity. Please try again.");
                return View(await BuildEditVM(model));
            }

            _logger.LogInformation("Activity {Id} '{Title}' edited by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Activity updated.";
            if (skipped.Any())
                TempData["Warning"] = "Some files were skipped: " + string.Join(" | ", skipped);
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.Activities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            a.IsVisible = !a.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle visibility for activity {Id}", id);
                TempData["Error"] = "Could not update visibility. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Visibility updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Activities.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (a == null) return NotFound();

            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId == null || a.DeptId != currentUser.DeptId)
                    return Forbid();
            }

            a.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete activity {Id}", id);
                TempData["Error"] = "Could not delete the activity. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Activity {Id} '{Title}' deleted by {User}", a.Id, a.Title, User.Identity?.Name);
            TempData["Success"] = "Activity deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE IMAGE ──────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int activityId)
        {
            if (!await CanManageActivity(activityId)) return Forbid();

            var img = await _context.ActivityImages.FindAsync(id);
            if (img == null || img.ActivityId != activityId) return NotFound();

            var imagePath = img.ImagePath;

            try
            {
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete activity image {ImageId} for activity {ActivityId}", id, activityId);
                TempData["Error"] = "Could not delete the image. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = activityId });
            }

            TryDeleteFile(imagePath, "deleted activity image");
            return RedirectToAction(nameof(Edit), new { id = activityId });
        }

        // ── DELETE FILE ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteActivityFile(int id, int activityId)
        {
            if (!await CanManageActivity(activityId)) return Forbid();

            var f = await _context.ActivityFiles.FindAsync(id);
            if (f == null || f.ActivityId != activityId) return NotFound();

            var filePath = f.FilePath;

            try
            {
                f.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete activity file {FileId} for activity {ActivityId}", id, activityId);
                TempData["Error"] = "Could not delete the file. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = activityId });
            }

            TryDeleteFile(filePath, "deleted activity file");
            return RedirectToAction(nameof(Edit), new { id = activityId });
        }

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// True if the current user is allowed to add/remove attachments on
        /// the given activity — SuperAdmin/ContentEditor/Principal always can;
        /// HOD only for their own department's activities.
        /// </summary>
        private async Task<bool> CanManageActivity(int activityId)
        {
            if (!User.IsInRole(AppRoles.HOD)) return true;

            var currentUser = await GetCurrentUserAsync();
            if (currentUser?.DeptId == null) return false;

            var deptId = await _context.Activities
                .Where(a => a.Id == activityId)
                .Select(a => a.DeptId)
                .FirstOrDefaultAsync();

            return deptId == currentUser.DeptId;
        }

        private async Task<ActivityCreateVM> BuildCreateVM(ActivityCreateVM vm)
        {
            await PopulateDropdowns(vm.Departments = new(), vm.Committees = new(), vm.Clubs = new());
            return vm;
        }

        private async Task<ActivityEditVM> BuildEditVM(ActivityEditVM vm)
        {
            await PopulateDropdowns(vm.Departments = new(), vm.Committees = new(), vm.Clubs = new());
            return vm;
        }

        private async Task PopulateDropdowns(List<SelectListItem> departments, List<SelectListItem> committees, List<SelectListItem> clubs)
        {
            departments.AddRange(await _context.Departments.OrderBy(d => d.Name).AsNoTracking()
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync());
            committees.AddRange(await _context.CampusCommittees.OrderBy(c => c.Title).AsNoTracking()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync());
            clubs.AddRange(await _context.StudentClubs.OrderBy(c => c.Title).AsNoTracking()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync());
        }

        private record FileUploadOutcome(string OriginalFileName, FileUploadResult Result);

        /// <summary>
        /// Saves every non-empty file posted under the given form field name
        /// (e.g. "Images", "Files") through the centralized file service.
        /// Shared by Create and Edit so the validate/save/track logic for
        /// multi-file inputs exists in exactly one place.
        /// </summary>
        private async Task<List<FileUploadOutcome>> SaveFormFilesAsync(string formFieldName, FileCategory category)
        {
            var outcomes = new List<FileUploadOutcome>();
            foreach (var file in Request.Form.Files.Where(f => f.Name == formFieldName))
            {
                if (file.Length == 0) continue;
                var result = await _fileStorage.SaveAsync(file, UploadFolder, category);
                outcomes.Add(new FileUploadOutcome(file.FileName, result));
            }
            return outcomes;
        }

        private static string ExtensionLabel(string fileName)
        {
            var ext = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();
            return string.IsNullOrEmpty(ext) ? "FILE" : ext;
        }

        /// <summary>
        /// Deletes a single file and never throws — a failed delete here
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
    }
}