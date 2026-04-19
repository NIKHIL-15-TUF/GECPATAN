//using GECPatan.Admin.Data;
//using GECPatan.Admin.Models.Domain;
//using GECPatan.Admin.Models.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;

//namespace GECPatan.Admin.Controllers
//{
//    [Authorize(Roles = "SuperAdmin,PlacementOfficer")]
//    public class PlacementController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public PlacementController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        // ── INDEX ─────────────────────────────────────────
//        public async Task<IActionResult> Index(string? branch, string? year)
//        {
//            ViewData["Title"] = "Placement Statistics";

//            var branches = await _context.PlacementStatistics
//                .Select(p => p.BranchName).Distinct().OrderBy(b => b).ToListAsync();
//            var years = await _context.PlacementStatistics
//                .Select(p => p.Year).Distinct().OrderByDescending(y => y).ToListAsync();

//            ViewBag.Branches = new SelectList(branches, branch);
//            ViewBag.Years = new SelectList(years, year);
//            ViewBag.SelectedBranch = branch;
//            ViewBag.SelectedYear = year;

//            var query = _context.PlacementStatistics.AsQueryable();
//            if (!string.IsNullOrEmpty(branch)) query = query.Where(p => p.BranchName == branch);
//            if (!string.IsNullOrEmpty(year)) query = query.Where(p => p.Year == year);

//            var list = await query
//                .OrderBy(p => p.BranchName)
//                .ThenByDescending(p => p.Year)
//                .Select(p => new PlacementStatisticListVM
//                {
//                    Id = p.Id,
//                    BranchName = p.BranchName,
//                    Year = p.Year,
//                    Passout = p.Passout,
//                    Placed = p.Placed,
//                    PlacementPercentage = p.PlacementPercentage,
//                    IsDisplay = p.IsDisplay
//                })
//                .ToListAsync();

//            return View(list);
//        }

//        // ── CREATE GET ────────────────────────────────────
//        public async Task<IActionResult> Create()
//        {
//            ViewData["Title"] = "Add Placement Record";
//            ViewBag.Departments = await GetDeptList();
//            return View(new PlacementStatisticVM
//            {
//                Year = DateTime.Now.Year.ToString()
//            });
//        }

//        // ── CREATE POST ───────────────────────────────────
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(PlacementStatisticVM model)
//        {
//            ViewData["Title"] = "Add Placement Record";
//            ViewBag.Departments = await GetDeptList();

//            if (!ModelState.IsValid) return View(model);

//            // Auto calc percentage if not set
//            if (model.PlacementPercentage == 0 && model.Passout > 0)
//                model.PlacementPercentage = Math.Round((double)model.Placed / model.Passout * 100, 2);

//            _context.PlacementStatistics.Add(new PlacementStatistic
//            {
//                BranchName = model.BranchName,
//                BranchId = model.BranchId,
//                Year = model.Year,
//                Passout = model.Passout,
//                Placed = model.Placed,
//                AverageCTC = model.AverageCTC,
//                HigherStudy = model.HigherStudy,
//                Business = model.Business,
//                PlacementPercentage = model.PlacementPercentage,
//                IsDisplay = model.IsDisplay
//            });

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Placement record added.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── EDIT GET ──────────────────────────────────────
//        public async Task<IActionResult> Edit(int id)
//        {
//            ViewData["Title"] = "Edit Placement Record";
//            ViewBag.Departments = await GetDeptList();

//            var p = await _context.PlacementStatistics.FindAsync(id);
//            if (p == null) return NotFound();

//            return View(new PlacementStatisticVM
//            {
//                Id = p.Id,
//                BranchName = p.BranchName,
//                BranchId = p.BranchId,
//                Year = p.Year,
//                Passout = p.Passout,
//                Placed = p.Placed,
//                AverageCTC = p.AverageCTC,
//                HigherStudy = p.HigherStudy,
//                Business = p.Business,
//                PlacementPercentage = p.PlacementPercentage,
//                IsDisplay = p.IsDisplay
//            });
//        }

//        // ── EDIT POST ─────────────────────────────────────
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, PlacementStatisticVM model)
//        {
//            ViewData["Title"] = "Edit Placement Record";
//            ViewBag.Departments = await GetDeptList();

//            if (!ModelState.IsValid) return View(model);

//            var p = await _context.PlacementStatistics.FindAsync(id);
//            if (p == null) return NotFound();

//            if (model.PlacementPercentage == 0 && model.Passout > 0)
//                model.PlacementPercentage = Math.Round((double)model.Placed / model.Passout * 100, 2);

//            p.BranchName = model.BranchName;
//            p.BranchId = model.BranchId;
//            p.Year = model.Year;
//            p.Passout = model.Passout;
//            p.Placed = model.Placed;
//            p.AverageCTC = model.AverageCTC;
//            p.HigherStudy = model.HigherStudy;
//            p.Business = model.Business;
//            p.PlacementPercentage = model.PlacementPercentage;
//            p.IsDisplay = model.IsDisplay;

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Placement record updated.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── TOGGLE DISPLAY ────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> ToggleDisplay(int id)
//        {
//            var p = await _context.PlacementStatistics.FindAsync(id);
//            if (p == null) return NotFound();
//            p.IsDisplay = !p.IsDisplay;
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        // ── DELETE ────────────────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var p = await _context.PlacementStatistics.FindAsync(id);
//            if (p == null) return NotFound();
//            p.IsDeleted = true;
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Record deleted.";
//            return RedirectToAction(nameof(Index));
//        }

//        private async Task<List<SelectListItem>> GetDeptList()
//        {
//            return await _context.Departments
//                .OrderBy(d => d.Name)
//                .Select(d => new SelectListItem
//                {
//                    Value = d.DeptId.ToString(),
//                    Text = d.Name
//                }).ToListAsync();
//        }
//    }
//}