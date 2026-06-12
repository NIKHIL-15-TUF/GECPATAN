//using GECPatan.Core.Data;
//using  GECPatan.Core.Models.Domain;
//using GECPatan.Admin.Models.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace GECPatan.Admin.Controllers
//{
//    [Authorize(Roles = "SuperAdmin,ContentEditor")]
//    public class TenderController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IWebHostEnvironment _env;

//        public TenderController(ApplicationDbContext context, IWebHostEnvironment env)
//        {
//            _context = context;
//            _env = env;
//        }

//        public async Task<IActionResult> Index()
//        {
//            ViewData["Title"] = "Tenders";
//            var items = await _context.Tenders
//                .OrderByDescending(t => t.Id)
//                .ToListAsync();
//            return View(items);
//        }

//        public IActionResult Create()
//        {
//            ViewData["Title"] = "Add Tender";
//            return View(new TenderVM { UploadDate = DateTime.Today.ToString("dd MMM yyyy") });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(TenderVM model, IFormFile? TenderFile)
//        {
//            ViewData["Title"] = "Add Tender";
//            if (!ModelState.IsValid) return View(model);

//            var tender = new Tender
//            {
//                Title = model.Title,
//                UploadDate = model.UploadDate,
//                IsVisible = model.IsVisible,
//                DisplayOrder = model.DisplayOrder
//            };

//            if (TenderFile != null && TenderFile.Length > 0)
//                tender.FilePath = await SaveFileAsync(TenderFile, "tenders");

//            _context.Tenders.Add(tender);
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Tender added.";
//            return RedirectToAction(nameof(Index));
//        }

//        public async Task<IActionResult> Edit(int id)
//        {
//            ViewData["Title"] = "Edit Tender";
//            var t = await _context.Tenders.FindAsync(id);
//            if (t == null) return NotFound();
//            return View(new TenderVM
//            {
//                Id = t.Id,
//                Title = t.Title,
//                UploadDate = t.UploadDate,
//                IsVisible = t.IsVisible,
//                DisplayOrder = t.DisplayOrder,
//                ExistingFilePath = t.FilePath
//            });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, TenderVM model, IFormFile? TenderFile)
//        {
//            ViewData["Title"] = "Edit Tender";
//            if (!ModelState.IsValid) return View(model);

//            var t = await _context.Tenders.FindAsync(id);
//            if (t == null) return NotFound();

//            t.Title = model.Title;
//            t.UploadDate = model.UploadDate;
//            t.IsVisible = model.IsVisible;
//            t.DisplayOrder = model.DisplayOrder;

//            if (TenderFile != null && TenderFile.Length > 0) { DeleteFile(t.FilePath); t.FilePath = await SaveFileAsync(TenderFile, "tenders"); }

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Tender updated.";
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> ToggleVisible(int id)
//        {
//            var t = await _context.Tenders.FindAsync(id);
//            if (t == null) return NotFound();
//            t.IsVisible = !t.IsVisible;
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var t = await _context.Tenders.FindAsync(id);
//            if (t == null) return NotFound();
//            t.IsDeleted = true;
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Tender deleted.";
//            return RedirectToAction(nameof(Index));
//        }

//        private async Task<string> SaveFileAsync(IFormFile file, string folder)
//        {
//            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folder);
//            Directory.CreateDirectory(uploadsFolder);
//            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//            var filePath = Path.Combine(uploadsFolder, fileName);
//            using var stream = new FileStream(filePath, FileMode.Create);
//            await file.CopyToAsync(stream);
//            return $"/uploads/{folder}/{fileName}";
//        }

//        private void DeleteFile(string? filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return;
//            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
//            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
//        }
//    }
//}
