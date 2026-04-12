using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class NewsItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NewsItemController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                    PublishDate = n.PublishDate.HasValue ? n.PublishDate.Value.ToString("dd MMM yyyy") : "",
                    ThumbnailPath = n.ThumbnailPath,
                    IsVisible = n.IsVisible,
                    ShowInMarquee = n.ShowInMarquee,
                    ImageCount = n.Images.Count,
                    FileCount = n.Files.Count
                })
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add News";
            return View(new NewsItemVM { PublishDate = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsItemVM model, IFormFile? Banner, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Add News";
            if (!ModelState.IsValid) return View(model);

            var news = new NewsItem
            {
                Title = model.Title,
                Description = model.Description,
                PublishDate = model.PublishDate,
                ExternalLink = model.ExternalLink,
                ControllerName = model.ControllerName,
                ActionName = model.ActionName,
                IsVisible = model.IsVisible,
                ShowInMarquee = model.ShowInMarquee
            };

            if (Banner != null && Banner.Length > 0)
                news.BannerImagePath = await SaveFileAsync(Banner, "news");
            if (Thumbnail != null && Thumbnail.Length > 0)
                news.ThumbnailPath = await SaveFileAsync(Thumbnail, "news");

            _context.NewsItems.Add(news);
            await _context.SaveChangesAsync();

            // Images
            int imgOrder = 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
                if (file.Length > 0)
                    _context.NewsItemImages.Add(new NewsItemImage
                    {
                        NewsItemId = news.Id,
                        ImagePath = await SaveFileAsync(file, "news"),
                        DisplayOrder = imgOrder++
                    });

            // Files
            int fileOrder = 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Files"))
                if (file.Length > 0)
                    _context.NewsItemFiles.Add(new NewsItemFile
                    {
                        NewsItemId = news.Id,
                        FilePath = await SaveFileAsync(file, "news"),
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        FileType = "PDF",
                        DisplayOrder = fileOrder++
                    });

            await _context.SaveChangesAsync();
            TempData["Success"] = "News item added.";
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

            ViewBag.ExistingImages = n.Images.OrderBy(i => i.DisplayOrder).ToList();
            ViewBag.ExistingFiles = n.Files.OrderBy(f => f.DisplayOrder).ToList();

            return View(new NewsItemVM
            {
                Id = n.Id,
                Title = n.Title,
                Description = n.Description,
                PublishDate = n.PublishDate,
                ExternalLink = n.ExternalLink,
                ControllerName = n.ControllerName,
                ActionName = n.ActionName,
                IsVisible = n.IsVisible,
                ShowInMarquee = n.ShowInMarquee,
                ExistingBannerPath = n.BannerImagePath,
                ExistingThumbnailPath = n.ThumbnailPath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsItemVM model, IFormFile? Banner, IFormFile? Thumbnail)
        {
            ViewData["Title"] = "Edit News";
            if (!ModelState.IsValid) return View(model);

            var n = await _context.NewsItems
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (n == null) return NotFound();

            n.Title = model.Title;
            n.Description = model.Description;
            n.PublishDate = model.PublishDate;
            n.ExternalLink = model.ExternalLink;
            n.ControllerName = model.ControllerName;
            n.ActionName = model.ActionName;
            n.IsVisible = model.IsVisible;
            n.ShowInMarquee = model.ShowInMarquee;

            if (Banner != null && Banner.Length > 0) { DeleteFile(n.BannerImagePath); n.BannerImagePath = await SaveFileAsync(Banner, "news"); }
            if (Thumbnail != null && Thumbnail.Length > 0) { DeleteFile(n.ThumbnailPath); n.ThumbnailPath = await SaveFileAsync(Thumbnail, "news"); }

            // Add new images
            int imgOrder = n.Images.Any() ? n.Images.Max(i => i.DisplayOrder) + 1 : 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
                if (file.Length > 0)
                    _context.NewsItemImages.Add(new NewsItemImage { NewsItemId = id, ImagePath = await SaveFileAsync(file, "news"), DisplayOrder = imgOrder++ });

            // Add new files
            int fileOrder = n.Files.Any() ? n.Files.Max(f => f.DisplayOrder) + 1 : 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Files"))
                if (file.Length > 0)
                    _context.NewsItemFiles.Add(new NewsItemFile { NewsItemId = id, FilePath = await SaveFileAsync(file, "news"), Title = Path.GetFileNameWithoutExtension(file.FileName), FileType = "PDF", DisplayOrder = fileOrder++ });

            await _context.SaveChangesAsync();
            TempData["Success"] = "News item updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var n = await _context.NewsItems.FindAsync(id);
            if (n == null) return NotFound();
            n.IsVisible = !n.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var n = await _context.NewsItems.FindAsync(id);
            if (n == null) return NotFound();
            n.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "News item deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, int newsId)
        {
            var img = await _context.NewsItemImages.FindAsync(id);
            if (img != null) { DeleteFile(img.ImagePath); img.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Edit), new { id = newsId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNewsFile(int id, int newsId)
        {
            var f = await _context.NewsItemFiles.FindAsync(id);
            if (f != null) { DeleteFile(f.FilePath); f.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Edit), new { id = newsId });
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
