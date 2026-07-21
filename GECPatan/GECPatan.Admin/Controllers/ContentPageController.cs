using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
 
namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principal")]
    public class ContentPageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notify;
        private readonly IWebHostEnvironment _env;

        public ContentPageController(ApplicationDbContext context, NotificationService notify, IWebHostEnvironment env)
        {
            _context = context;
            _notify = notify;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Content Pages";
            var pages = await _context.ContentPages
                .OrderByDescending(p => p.CreatedDate)
                .Select(p => new ContentPageListVM
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    CreatedBy = p.CreatedBy,
                    CreatedDate = p.CreatedDate,
                    IsVisible = p.IsVisible
                })
                .ToListAsync();

            return View(pages);
        }

        // ── CREATE GET ────────────────────────────────────
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Page";
            return View(new ContentPageCreateVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContentPageCreateVM model)
        {
            ViewData["Title"] = "Create Page";

            // Auto-generate slug if not provided
            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = GenerateSlug(model.Title);
            else
                model.Slug = GenerateSlug(model.Slug);

            // Check slug is unique
            if (await _context.ContentPages.AnyAsync(p => p.Slug == model.Slug))
            {
                ModelState.AddModelError("Slug",
                    "This slug is already in use. Please choose a different title or slug.");
            }

            if (!ModelState.IsValid) return View(model);

            var userName = User.Identity?.Name ?? "Admin";

            var page = new ContentPage
            {
                Title = model.Title,
                Slug = model.Slug,
                HtmlContent = model.HtmlContent,
                CreatedBy = userName,
                IsVisible = model.IsVisible
            };

            _context.ContentPages.Add(page);
            await _context.SaveChangesAsync();

            // Save carousel images (separate from HtmlContent)
            await SaveCarouselImages(page.Id, page.Slug, model.CarouselImages);

            // Audit log
            await WriteAuditLog("Created", "ContentPage", page.Id, page.Title);
            //Notificcation Send
            await _notify.SendAsync(
                title: $"Content Page Published: {page.Title}",
                message: $"URL: /page/{page.Slug}",
                module: "ContentPage",
                icon: "fa-file-alt",
                color: "primary",
                link: $"/ContentPage/Edit/{page.Id}",
                forRole: "SuperAdmin"
            );
            TempData["Success"] = $"Page '{page.Title}' created. URL: /page/{page.Slug}";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Page";
            var p = await _context.ContentPages
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            return View(new ContentPageEditVM
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                HtmlContent = p.HtmlContent,
                IsVisible = p.IsVisible,
                GeneratedUrl = $"/page/{p.Slug}",
                ExistingImages = p.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    }).ToList()
            });
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContentPageEditVM model)
        {
            ViewData["Title"] = "Edit Page";

            if (string.IsNullOrWhiteSpace(model.Slug))
                model.Slug = GenerateSlug(model.Title);
            else
                model.Slug = GenerateSlug(model.Slug);

            // Unique slug check (excluding self)
            if (await _context.ContentPages
                .AnyAsync(p => p.Slug == model.Slug && p.Id != id))
            {
                ModelState.AddModelError("Slug",
                    "This slug is already in use by another page.");
            }

            if (!ModelState.IsValid)
            {
                var existing = await _context.ContentPages.Include(x => x.Images)
                    .FirstOrDefaultAsync(x => x.Id == id);
                model.ExistingImages = existing?.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ContentPageImageVM
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder
                    }).ToList() ?? new List<ContentPageImageVM>();
                return View(model);
            }

            var p = await _context.ContentPages.FindAsync(id);
            if (p == null) return NotFound();

            p.Title = model.Title;
            p.Slug = model.Slug;
            p.HtmlContent = model.HtmlContent;
            p.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();

            // Append any newly-picked carousel images
            await SaveCarouselImages(p.Id, p.Slug, model.CarouselImages);

            await WriteAuditLog("Updated", "ContentPage", p.Id, p.Title);

            TempData["Success"] = $"Page updated. URL: /page/{p.Slug}";
            if (User.IsInRole(AppRoles.ContentEditor))
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ONE CAROUSEL IMAGE ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int pageId)
        {
            var img = await _context.ContentPageImages.FindAsync(id);
            if (img != null)
            {
                var physicalPath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
                _context.ContentPageImages.Remove(img);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Image removed.";
            }
            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var p = await _context.ContentPages.FindAsync(id);
            if (p == null) return NotFound();
            p.IsVisible = !p.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{p.Title}' " + (p.IsVisible ? "published" : "hidden") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.ContentPages.FindAsync(id);
            if (p == null) return NotFound();
            p.IsDeleted = true;
            await _context.SaveChangesAsync();
            await WriteAuditLog("Deleted", "ContentPage", p.Id, p.Title);
            TempData["Success"] = $"Page '{p.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── PREVIEW (renders TinyMCE content + carousel) ──
        public async Task<IActionResult> Preview(int id)
        {
            var p = await _context.ContentPages
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();
            ViewData["Title"] = p.Title;
            return View(p);
        }

        // ── SLUG GENERATOR ────────────────────────────────
        private static string GenerateSlug(string text)
        {
            // Lowercase
            text = text.ToLowerInvariant();
            // Replace spaces with hyphens
            text = Regex.Replace(text, @"\s+", "-");
            // Remove non-alphanumeric except hyphens
            text = Regex.Replace(text, @"[^a-z0-9\-]", "");
            // Remove consecutive hyphens
            text = Regex.Replace(text, @"-+", "-");
            // Trim hyphens from ends
            text = text.Trim('-');
            return text;
        }

        // ── CAROUSEL IMAGE UPLOAD HELPER ──────────────────
        private async Task SaveCarouselImages(int contentPageId, string slug, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0) return;

            var folderRelative = $"/uploads/content-pages/{slug}";
            var folderPhysical = Path.Combine(_env.WebRootPath, "uploads", "content-pages", slug);
            Directory.CreateDirectory(folderPhysical);

            var nextOrder = await _context.ContentPageImages
                .Where(i => i.ContentPageId == contentPageId)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync() ?? -1;
            nextOrder++;

            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext)) continue;

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var physicalPath = Path.Combine(folderPhysical, fileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.ContentPageImages.Add(new ContentPageImage
                {
                    ContentPageId = contentPageId,
                    ImageUrl = $"{folderRelative}/{fileName}",
                    DisplayOrder = nextOrder
                });
                nextOrder++;
            }

            await _context.SaveChangesAsync();
        }

        // ── AUDIT HELPER ──────────────────────────────────
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