using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class NewsItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly NotificationService _notify;
        private readonly ILogger<NewsItemController> _logger;
        private const string NewsFolder = "news";

        public NewsItemController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            NotificationService notify,
            ILogger<NewsItemController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _notify = notify;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "News & Notices";
            var items = await _context.NewsItems
                .Include(n => n.Images)
                .Include(n => n.Files)
                .OrderByDescending(n => n.PublishDate)
                .Select(n => new NewsItemListVM
                {
                    Id = n.Id,
                    Title = n.Title,
                    PublishDate = n.PublishDate.HasValue
                                    ? n.PublishDate.Value.ToString("dd MMM yyyy") : "",
                    ThumbnailPath = n.ThumbnailPath,
                    IsVisible = n.IsVisible,
                    ShowInMarquee = n.ShowInMarquee,
                    ImageCount = n.Images.Count,
                    FileCount = n.Files.Count
                })
                .ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add News";
            return View(await BuildVM(new NewsItemVM
            {
                PublishDate = DateTime.Today
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsItemVM model, IFormFile? Banner, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Add News";
            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            var imageFiles = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();
            var docFiles = Request.Form.Files.Where(f => f.Name == "Files" && f.Length > 0).ToList();

            // Validate & save every file up front — banner, thumbnail, gallery images,
            // and attached documents — before any database write, so a rejected file
            // never leaves a partially-saved news item.
            var savedPaths = new List<string>();

            async Task<string?> TrySaveAsync(IFormFile? file, FileCategory category, string fieldName)
            {
                if (file == null || file.Length == 0) return null;

                var result = await _fileStorage.SaveAsync(file, NewsFolder, category);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(fieldName, result.ErrorMessage!);
                    return null;
                }

                savedPaths.Add(result.RelativePath!);
                return result.RelativePath;
            }

            string? bannerPath = await TrySaveAsync(Banner, FileCategory.Image, nameof(Banner));
            if (!ModelState.IsValid) return View(await BuildVM(model));

            string? thumbnailPath = await TrySaveAsync(Thumbnail, FileCategory.Image, nameof(Thumbnail));
            if (!ModelState.IsValid) return View(await BuildVM(model));

            var galleryImages = new List<string>();
            foreach (var file in imageFiles)
            {
                var path = await TrySaveAsync(file, FileCategory.Image, string.Empty);
                if (!ModelState.IsValid) return View(await BuildVM(model));
                galleryImages.Add(path!);
            }

            var attachedFiles = new List<(string Path, string Title)>();
            foreach (var file in docFiles)
            {
                var path = await TrySaveAsync(file, FileCategory.Document, string.Empty);
                if (!ModelState.IsValid) return View(await BuildVM(model));
                attachedFiles.Add((path!, Path.GetFileNameWithoutExtension(file.FileName)));
            }

            var news = new NewsItem
            {
                Title = model.Title,
                Description = model.Description,
                PublishDate = model.PublishDate,
                ExternalLink = model.ExternalLink,
                ControllerName = model.LinkDeptId.HasValue
                    ? "Department" : (model.LinkCommitteeId.HasValue ? "CampusCommittee" : null),
                ActionName = model.LinkDeptId.HasValue
                    ? model.LinkDeptId.ToString() : (model.LinkCommitteeId.HasValue ? model.LinkCommitteeId.ToString() : null),
                IsVisible = model.IsVisible,
                ShowInMarquee = model.ShowInMarquee,
                BannerImagePath = bannerPath,
                ThumbnailPath = thumbnailPath
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.NewsItems.Add(news);
                await _context.SaveChangesAsync();

                int imgOrder = 0;
                foreach (var path in galleryImages)
                {
                    _context.NewsItemImages.Add(new NewsItemImage
                    {
                        NewsItemId = news.Id,
                        ImagePath = path,
                        DisplayOrder = imgOrder++
                    });
                }

                int fileOrder = 0;
                foreach (var (path, title) in attachedFiles)
                {
                    _context.NewsItemFiles.Add(new NewsItemFile
                    {
                        NewsItemId = news.Id,
                        FilePath = path,
                        Title = title,
                        FileType = "PDF",
                        DisplayOrder = fileOrder++
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "News item {NewsId} '{Title}' created with {ImageCount} image(s), {FileCount} file(s)",
                    news.Id, news.Title, galleryImages.Count, attachedFiles.Count);

                TempData["Success"] = "News item added.";
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error creating news item '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the news item. Please try again.");
                return View(await BuildVM(model));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error creating news item '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(await BuildVM(model));
            }

            // Notification failures shouldn't roll back an already-committed news item
            // or surface as an unhandled 500 to the editor who just successfully saved it.
            try
            {
                await _notify.SendAsync(
                    title: $"New News: {news.Title}",
                    message: null,
                    module: "News",
                    icon: "fa-newspaper",
                    color: "primary",
                    link: $"/NewsItem/Edit/{news.Id}",
                    forRole: "SuperAdmin"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification for news item {NewsId}", news.Id);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit News";
            var n = await _context.NewsItems
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (n == null) return NotFound();

            ViewBag.ExistingImages = n.Images.Where(i => !i.IsDeleted).OrderBy(i => i.DisplayOrder).ToList();
            ViewBag.ExistingFiles = n.Files.Where(f => !f.IsDeleted).OrderBy(f => f.DisplayOrder).ToList();

            int? linkDeptId = null;
            int? linkCommitteeId = null;
            if (n.ControllerName == "Department" && int.TryParse(n.ActionName, out int dId))
                linkDeptId = dId;
            else if (n.ControllerName == "CampusCommittee" && int.TryParse(n.ActionName, out int cId))
                linkCommitteeId = cId;

            var vm = new NewsItemVM
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                PublishDate = n.PublishDate,
                ExternalLink = n.ExternalLink,
                LinkDeptId = linkDeptId,
                LinkCommitteeId = linkCommitteeId,
                IsVisible = n.IsVisible,
                ShowInMarquee = n.ShowInMarquee,
                ExistingBannerPath = n.BannerImagePath,
                ExistingThumbnailPath = n.ThumbnailPath
            };

            return View(await BuildVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsItemVM model, IFormFile? Banner, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Edit News";
            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            var n = await _context.NewsItems
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (n == null) return NotFound();

            var imageFiles = Request.Form.Files.Where(f => f.Name == "Images" && f.Length > 0).ToList();
            var docFiles = Request.Form.Files.Where(f => f.Name == "Files" && f.Length > 0).ToList();

            var savedPaths = new List<string>();

            async Task<string?> TrySaveAsync(IFormFile? file, FileCategory category, string fieldName)
            {
                if (file == null || file.Length == 0) return null;

                var result = await _fileStorage.SaveAsync(file, NewsFolder, category);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(fieldName, result.ErrorMessage!);
                    return null;
                }

                savedPaths.Add(result.RelativePath!);
                return result.RelativePath;
            }

            bool replacingBanner = Banner != null && Banner.Length > 0;
            string? newBannerPath = await TrySaveAsync(Banner, FileCategory.Image, nameof(Banner));
            if (!ModelState.IsValid) return View(await BuildVM(model));

            bool replacingThumbnail = Thumbnail != null && Thumbnail.Length > 0;
            string? newThumbnailPath = await TrySaveAsync(Thumbnail, FileCategory.Image, nameof(Thumbnail));
            if (!ModelState.IsValid) return View(await BuildVM(model));

            var galleryImages = new List<string>();
            foreach (var file in imageFiles)
            {
                var path = await TrySaveAsync(file, FileCategory.Image, string.Empty);
                if (!ModelState.IsValid) return View(await BuildVM(model));
                galleryImages.Add(path!);
            }

            var attachedFiles = new List<(string Path, string Title)>();
            foreach (var file in docFiles)
            {
                var path = await TrySaveAsync(file, FileCategory.Document, string.Empty);
                if (!ModelState.IsValid) return View(await BuildVM(model));
                attachedFiles.Add((path!, Path.GetFileNameWithoutExtension(file.FileName)));
            }

            string? previousBannerPath = n.BannerImagePath;
            string? previousThumbnailPath = n.ThumbnailPath;

            n.Title = model.Title;
            n.Description = model.Description;
            n.PublishDate = model.PublishDate;
            n.ExternalLink = model.ExternalLink;
            n.IsVisible = model.IsVisible;
            n.ShowInMarquee = model.ShowInMarquee;

            n.ControllerName = model.LinkDeptId.HasValue
                ? "Department" : (model.LinkCommitteeId.HasValue ? "CampusCommittee" : null);
            n.ActionName = model.LinkDeptId.HasValue
                ? model.LinkDeptId.ToString() : (model.LinkCommitteeId.HasValue ? model.LinkCommitteeId.ToString() : null);

            if (replacingBanner) n.BannerImagePath = newBannerPath;
            if (replacingThumbnail) n.ThumbnailPath = newThumbnailPath;

            int imgOrder = n.Images.Any(i => !i.IsDeleted) ? n.Images.Where(i => !i.IsDeleted).Max(i => i.DisplayOrder) + 1 : 0;
            int fileOrder = n.Files.Any(f => !f.IsDeleted) ? n.Files.Where(f => !f.IsDeleted).Max(f => f.DisplayOrder) + 1 : 0;

            try
            {
                foreach (var path in galleryImages)
                {
                    _context.NewsItemImages.Add(new NewsItemImage
                    {
                        NewsItemId = id,
                        ImagePath = path,
                        DisplayOrder = imgOrder++
                    });
                }

                foreach (var (path, title) in attachedFiles)
                {
                    _context.NewsItemFiles.Add(new NewsItemFile
                    {
                        NewsItemId = id,
                        FilePath = path,
                        Title = title,
                        FileType = "PDF",
                        DisplayOrder = fileOrder++
                    });
                }

                await _context.SaveChangesAsync();

                // Old banner/thumbnail removed only after the new state is safely persisted.
                if (replacingBanner) _fileStorage.Delete(previousBannerPath);
                if (replacingThumbnail) _fileStorage.Delete(previousThumbnailPath);

                _logger.LogInformation("News item {NewsId} updated with {NewImageCount} new image(s), {NewFileCount} new file(s)",
                    id, galleryImages.Count, attachedFiles.Count);

                TempData["Success"] = "News item updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error updating news item {NewsId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the news item. Please try again.");
                return View(await BuildVM(model));
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error updating news item {NewsId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(await BuildVM(model));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var n = await _context.NewsItems.FindAsync(id);
            if (n == null) return NotFound();

            n.IsVisible = !n.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("News item {NewsId} visibility set to {IsVisible}", id, n.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for news item {NewsId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.NewsItems
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (n == null) return NotFound();

            var pathsToDelete = new List<string?> { n.BannerImagePath, n.ThumbnailPath };
            pathsToDelete.AddRange(n.Images.Select(i => i.ImagePath));
            pathsToDelete.AddRange(n.Files.Select(f => f.FilePath));

            n.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                foreach (var path in pathsToDelete)
                    _fileStorage.Delete(path);

                _logger.LogInformation("News item {NewsId} '{Title}' deleted", id, n.Title);
                TempData["Success"] = "News item deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting news item {NewsId}", id);
                TempData["Error"] = "Unable to delete the news item. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int newsId)
        {
            var img = await _context.NewsItemImages.FindAsync(id);
            if (img == null) return NotFound();

            string? imagePath = img.ImagePath;
            img.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                _fileStorage.Delete(imagePath);
                _logger.LogInformation("Image {ImageId} removed from news item {NewsId}", id, newsId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error removing image {ImageId} from news item {NewsId}", id, newsId);
                TempData["Error"] = "Unable to remove the image. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = newsId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNewsFile(int id, int newsId)
        {
            var f = await _context.NewsItemFiles.FindAsync(id);
            if (f == null) return NotFound();

            string? filePath = f.FilePath;
            f.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                _fileStorage.Delete(filePath);
                _logger.LogInformation("File {FileId} removed from news item {NewsId}", id, newsId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error removing file {FileId} from news item {NewsId}", id, newsId);
                TempData["Error"] = "Unable to remove the file. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = newsId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<NewsItemVM> BuildVM(NewsItemVM vm)
        {
            vm.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.Committees = await _context.CampusCommittees
                .OrderBy(c => c.Title)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Title
                }).ToListAsync();

            return vm;
        }
    }
}