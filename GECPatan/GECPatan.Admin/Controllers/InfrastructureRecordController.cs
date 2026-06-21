using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class InfrastructureRecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly string[] RoomTypeOptions = new[]
        {
            "Classroom", "Tutorial Room", "Laboratory",
            "Drawing Hall", "Computer Center",
            "Library & Reading Room", "Seminar Hall", "Other"
        };

        public InfrastructureRecordController(ApplicationDbContext context)
            => _context = context;

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Infrastructure Records";
            var records = await _context.InfrastructureRecords
                .Include(i => i.Department)
                .OrderBy(i => i.RoomType)
                .ThenBy(i => i.DisplayOrder)
                .ToListAsync();
            return View(records);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Infrastructure Record";
            var vm = new InfrastructureRecordVM();
            await LoadLists(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InfrastructureRecordVM model)
        {
            ViewData["Title"] = "Add Infrastructure Record";
            if (!ModelState.IsValid)
            {
                await LoadLists(model);
                return View("Form", model);
            }

            _context.InfrastructureRecords.Add(new InfrastructureRecord
            {
                RoomType = model.RoomType,
                AreaSqm = model.AreaSqm,
                BuildingName = model.BuildingName,
                DeptId = model.DeptId,
                DisplayOrder = model.DisplayOrder,
                IsVisible = model.IsVisible
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Infrastructure record added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Infrastructure Record";
            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();

            var vm = new InfrastructureRecordVM
            {
                Id = r.Id,
                RoomType = r.RoomType,
                AreaSqm = r.AreaSqm,
                BuildingName = r.BuildingName,
                DeptId = r.DeptId,
                DisplayOrder = r.DisplayOrder,
                IsVisible = r.IsVisible
            };
            await LoadLists(vm);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InfrastructureRecordVM model)
        {
            ViewData["Title"] = "Edit Infrastructure Record";
            if (!ModelState.IsValid)
            {
                await LoadLists(model);
                return View("Form", model);
            }

            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();

            r.RoomType = model.RoomType;
            r.AreaSqm = model.AreaSqm;
            r.BuildingName = model.BuildingName;
            r.DeptId = model.DeptId;
            r.DisplayOrder = model.DisplayOrder;
            r.IsVisible = model.IsVisible;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Infrastructure record updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.InfrastructureRecords.FindAsync(id);
            if (r == null) return NotFound();
            r.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Infrastructure record deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLists(InfrastructureRecordVM vm)
        {
            vm.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.RoomTypes = RoomTypeOptions
                .Select(t => new SelectListItem { Value = t, Text = t })
                .ToList();
        }
    }
}