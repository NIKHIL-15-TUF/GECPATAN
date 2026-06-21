using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    // ════════════════════════════════════════════════════════
    // STRICT SCHEMA NOTICE:
    // This controller deliberately has NO Create action and
    // NO Delete action. The 15 narrative sections are fixed
    // by DisclosureSectionKeys.AllKeys and are seeded once at
    // startup (see DisclosureNarrativeSeeder). Admin can only
    // EDIT content + toggle visibility for these existing
    // sections — never add a 16th, never remove one.
    // ════════════════════════════════════════════════════════
    [Authorize(Roles = "SuperAdmin")]
    public class DisclosureNarrativeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisclosureNarrativeController(ApplicationDbContext context)
            => _context = context;

        // ── INDEX — fixed list of 15 sections ──────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Narrative Sections — Mandatory Disclosure";

            var sections = await _context.DisclosureNarratives
                .OrderBy(n => n.DisplayOrder)
                .Select(n => new DisclosureNarrativeListVM
                {
                    Id = n.Id,
                    SectionKey = n.SectionKey,
                    SectionTitle = n.SectionTitle,
                    HasContent = !string.IsNullOrEmpty(n.HtmlContent),
                    IsVisible = n.IsVisible,
                    DisplayOrder = n.DisplayOrder,
                    LastUpdated = n.LastUpdated,
                    UpdatedBy = n.UpdatedBy
                })
                .ToListAsync();

            // Safety check — if seeder hasn't run yet, show a
            // clear message instead of an empty/broken page
            ViewBag.ExpectedCount = DisclosureSectionKeys.AllKeys.Length;
            ViewBag.ActualCount = sections.Count;

            return View(sections);
        }

        // ── EDIT GET — only action that changes data ───────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Narrative Section";

            var n = await _context.DisclosureNarratives.FindAsync(id);
            if (n == null) return NotFound();

            var vm = new DisclosureNarrativeEditVM
            {
                Id = n.Id,
                SectionKey = n.SectionKey,
                SectionTitle = n.SectionTitle,
                HtmlContent = n.HtmlContent,
                IsVisible = n.IsVisible
            };

            return View(vm);
        }

        // ── EDIT POST ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DisclosureNarrativeEditVM model)
        {
            ViewData["Title"] = "Edit Narrative Section";

            if (!ModelState.IsValid)
                return View(model);

            var n = await _context.DisclosureNarratives.FindAsync(id);
            if (n == null) return NotFound();

            // SectionKey and SectionTitle are intentionally NOT
            // editable here — strict schema. Only content +
            // visibility can change.
            n.HtmlContent = model.HtmlContent;
            n.IsVisible = model.IsVisible;
            n.LastUpdated = DateTime.Now;
            n.UpdatedBy = User.Identity?.Name ?? "Admin";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{n.SectionTitle}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // NOTE: No Create() action.
        // NOTE: No Delete() action.
        // This is intentional — see strict schema notice above.
    }
}