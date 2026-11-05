using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,Principle")]
    public class NotableAlumniController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<NotableAlumniController> _logger;

        private const string AlumniFolder = "notable-alumni";

        public NotableAlumniController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<NotableAlumniController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ================= INDEX =================

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Notable Alumni";

            var items = await _context.NotableAlumni
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.OrderNo)
                .ToListAsync();

            var list = items.Select(x => new NotableAlumniVM
            {
                ID = x.ID,
                OrderNo = x.OrderNo,
                IsShow = x.IsShow,
                IsIframe = x.IsIframe,
                IFrame = x.IFrame,
                Title = x.Title,
                Image = x.Image
            }).ToList();

            return View(list);
        }

        // ================= CREATE GET =================

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Notable Alumni";

            return View("Form", new NotableAlumniVM
            {
                IsShow = true,
                IsIframe = false
            });
        }

        // ================= CREATE POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            NotableAlumniVM model)
        {
            ViewData["Title"] = "Add Notable Alumni";

            if (!ModelState.IsValid)
                return View("Form", model);

            string? imagePath = null;

            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    model.ImageFile,
                    AlumniFolder,
                    FileCategory.Image);

                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        uploadResult.ErrorMessage
                            ?? "Unable to upload image.");

                    return View("Form", model);
                }

                imagePath = uploadResult.RelativePath;
            }

            var maxOrder = await _context.NotableAlumni
                .Where(x => !x.IsDeleted)
                .Select(x => (int?)x.OrderNo)
                .MaxAsync() ?? 0;

            var item = new NotableAlumni
            {
                OrderNo = model.OrderNo > 0
                    ? model.OrderNo
                    : maxOrder + 1,

                IsShow = model.IsShow,
                IsIframe = model.IsIframe,
                IFrame = model.IsIframe ? model.IFrame : null,
                Title = model.Title,
                Image = imagePath,

                IsDeleted = false,

                CreatedDate = DateTime.Now,
                CreatedDateInt = DateTimeOffset.Now.ToUnixTimeSeconds(),
                CreatedBy = User.Identity?.Name
            };

            try
            {
                _context.NotableAlumni.Add(item);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Notable Alumni {AlumniId} '{Title}' created",
                    item.ID,
                    item.Title);

                TempData["Success"] =
                    $"'{item.Title}' added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (imagePath != null)
                    _fileStorage.Delete(imagePath);

                _logger.LogError(
                    ex,
                    "Database error creating Notable Alumni");

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save Notable Alumni.");

                return View("Form", model);
            }
        }

        // ================= EDIT GET =================

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Notable Alumni";

            var item = await _context.NotableAlumni
                .FirstOrDefaultAsync(x =>
                    x.ID == id && !x.IsDeleted);

            if (item == null)
                return NotFound();

            var model = EntityToVM(item);

            return View("Form", model);
        }

        // ================= EDIT POST =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            NotableAlumniVM model)
        {
            ViewData["Title"] = "Edit Notable Alumni";

            if (id != model.ID)
                return BadRequest();

            if (!ModelState.IsValid)
                return View("Form", model);

            var item = await _context.NotableAlumni
                .FirstOrDefaultAsync(x =>
                    x.ID == id && !x.IsDeleted);

            if (item == null)
                return NotFound();

            string? newImagePath = null;

            bool replacingImage =
                model.ImageFile != null &&
                model.ImageFile.Length > 0;

            if (replacingImage)
            {
                var uploadResult = await _fileStorage.SaveAsync(
                    model.ImageFile!,
                    AlumniFolder,
                    FileCategory.Image);

                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(
                        nameof(model.ImageFile),
                        uploadResult.ErrorMessage
                            ?? "Unable to upload image.");

                    model.Image = item.Image;

                    return View("Form", model);
                }

                newImagePath = uploadResult.RelativePath;
            }

            string? previousImagePath = item.Image;

            item.OrderNo = model.OrderNo;
            item.IsShow = model.IsShow;
            item.IsIframe = model.IsIframe;
            item.IFrame = model.IsIframe ? model.IFrame : null;
            item.Title = model.Title;

            if (replacingImage)
                item.Image = newImagePath;

            item.UpdatedDate = DateTime.Now;
            item.UpdatedDateInt =
                DateTimeOffset.Now.ToUnixTimeSeconds();

            item.UpdatedBy = User.Identity?.Name;

            try
            {
                await _context.SaveChangesAsync();

                // Delete old image only after DB update succeeds.
                if (replacingImage)
                    _fileStorage.Delete(previousImagePath);

                _logger.LogInformation(
                    "Notable Alumni {AlumniId} updated",
                    item.ID);

                TempData["Success"] =
                    $"'{item.Title}' updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                if (replacingImage)
                    _fileStorage.Delete(newImagePath);

                _logger.LogError(
                    ex,
                    "Database error updating Notable Alumni {AlumniId}",
                    id);

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update Notable Alumni.");

                model.Image = previousImagePath;

                return View("Form", model);
            }
        }

        // ================= TOGGLE VISIBILITY =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var item = await _context.NotableAlumni
                .FirstOrDefaultAsync(x =>
                    x.ID == id && !x.IsDeleted);

            if (item == null)
                return NotFound();

            item.IsShow = !item.IsShow;

            item.UpdatedDate = DateTime.Now;
            item.UpdatedDateInt =
                DateTimeOffset.Now.ToUnixTimeSeconds();

            item.UpdatedBy = User.Identity?.Name;

            try
            {
                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"'{item.Title}' " +
                    (item.IsShow ? "shown" : "hidden") +
                    " successfully.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Error toggling visibility for Alumni {AlumniId}",
                    id);

                TempData["Error"] =
                    "Unable to update visibility.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= SOFT DELETE =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.NotableAlumni
                .FirstOrDefaultAsync(x =>
                    x.ID == id && !x.IsDeleted);

            if (item == null)
                return NotFound();

            item.IsDeleted = true;

            item.UpdatedDate = DateTime.Now;
            item.UpdatedDateInt =
                DateTimeOffset.Now.ToUnixTimeSeconds();

            item.UpdatedBy = User.Identity?.Name;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Notable Alumni {AlumniId} '{Title}' soft deleted",
                    item.ID,
                    item.Title);

                TempData["Success"] =
                    $"'{item.Title}' deleted successfully.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database error deleting Alumni {AlumniId}",
                    id);

                TempData["Error"] =
                    "Unable to delete Notable Alumni.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ================= HELPERS =================

        private static NotableAlumniVM EntityToVM(
            NotableAlumni item)
        {
            return new NotableAlumniVM
            {
                ID = item.ID,
                OrderNo = item.OrderNo,
                IsShow = item.IsShow,
                IsIframe = item.IsIframe,
                IFrame = item.IFrame,
                Title = item.Title,
                Image = item.Image
            };
        }
    }
}
