using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class VideoCarouselController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VideoCarouselController> _logger;

        public VideoCarouselController(
            ApplicationDbContext context,
            ILogger<VideoCarouselController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: VideoCarousel
        public async Task<IActionResult> Index()
        {
            var videos = await _context.VideoCarousel
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.OrderNo)
                .ToListAsync();

            return View(videos);
        }

        // GET: VideoCarousel/Create
        public IActionResult Create()
        {
            return View("Form", new VideoCarouselVM());
        }

        // POST: VideoCarousel/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoCarouselVM model)
        {
            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var video = new VideoCarousel
            {
                OrderNo = model.OrderNo,
                IsShow = model.IsShow,
                Title = model.Title,
                VideoUrl = model.VideoUrl,

                IsDeleted = false,
                CreatedDate = DateTime.Now,
                CreatedDateInt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                CreatedBy = User.Identity?.Name
            };

            _context.VideoCarousel.Add(video);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Video added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: VideoCarousel/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var video = await _context.VideoCarousel
                .FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);

            if (video == null)
            {
                return NotFound();
            }

            var model = new VideoCarouselVM
            {
                ID = video.ID,
                OrderNo = video.OrderNo,
                IsShow = video.IsShow,
                Title = video.Title,
                VideoUrl = video.VideoUrl,
                Image = video.Image
            };

            return View("Form", model);
        }

        // POST: VideoCarousel/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VideoCarouselVM model)
        {
            if (!ModelState.IsValid)
            {
                return View("Form", model);
            }

            var video = await _context.VideoCarousel
                .FirstOrDefaultAsync(x => x.ID == model.ID && !x.IsDeleted);

            if (video == null)
            {
                return NotFound();
            }

            video.OrderNo = model.OrderNo;
            video.IsShow = model.IsShow;
            video.Title = model.Title;
            video.VideoUrl = model.VideoUrl;

            video.UpdatedDate = DateTime.Now;
            video.UpdatedDateInt = DateTimeOffset.Now.ToUnixTimeSeconds();
            video.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Video updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: VideoCarousel/ToggleVisible/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var video = await _context.VideoCarousel
                .FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);

            if (video == null)
            {
                return NotFound();
            }

            video.IsShow = !video.IsShow;

            video.UpdatedDate = DateTime.Now;
            video.UpdatedDateInt = DateTimeOffset.Now.ToUnixTimeSeconds();
            video.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Video visibility updated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: VideoCarousel/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var video = await _context.VideoCarousel
                .FirstOrDefaultAsync(x => x.ID == id && !x.IsDeleted);

            if (video == null)
            {
                return NotFound();
            }

            // Soft delete
            video.IsDeleted = true;

            video.UpdatedDate = DateTime.Now;
            video.UpdatedDateInt = DateTimeOffset.Now.ToUnixTimeSeconds();
            video.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Video deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}