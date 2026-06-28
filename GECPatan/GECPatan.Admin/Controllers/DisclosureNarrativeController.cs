using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    // ════════════════════════════════════════════════════════
    // STRICT SCHEMA — only SuperAdmin can manage narratives
    // NO Create, NO Delete — only Edit existing sections
    // ════════════════════════════════════════════════════════
    [Authorize(Roles = "SuperAdmin")]
    public class DisclosureNarrativeController : Controller
    {
        private readonly ApplicationDbContext _context;

        // SiteSettings-backed sections (TinyMCE direct, not in
        // DisclosureNarratives table)
        //private static readonly Dictionary<string, (string Title, string Group, string Help)>
        //    _settingsKeys = new()
        //    {
        //        ["Disclosure.AboutInstitute"] = ("About the Institute", "About", "Write a general overview of the institute — history, location, vision, achievements."),
        //        ["Disclosure.AccreditationStatus"] = ("Accreditation Status Details", "Programs", "Add additional narrative about accreditation, NAAC grade, or other certifications."),
        //        ["Disclosure.PlacementFacilities"] = ("Placement Facilities", "Placement", "Describe the Training & Placement Cell, infrastructure, tie-ups with companies."),
        //        ["Disclosure.ForeignCollaboration"] = ("Foreign Collaboration Details", "Placement", "List any MoUs or collaborations with foreign universities/institutions."),
        //        ["Disclosure.FeeStructure"] = ("Fee Structure", "Fee", "Enter fee details for all programs (tuition, hostel, other fees) in tabular form."),
        //        ["Disclosure.AdmissionProcess"] = ("Admission Process", "Admission", "Describe the admission process followed (ACPC/GUJCET norms)."),
        //        ["Disclosure.CriteriaWeightages"] = ("Criteria & Weightages for Admission", "Admission", "List criteria and weightages used for admission (merit, reservation, etc.)."),
        //        ["Disclosure.ApplicantList"] = ("List of Applicants", "Admission", "Paste or link the list of applicants for the current year."),
        //        ["Disclosure.ManagementSeatsResult"] = ("Result of Admission under Management/Vacant Seats", "Admission", "Enter results of admission under management quota or vacant seats."),
        //        ["Disclosure.InfrastructureInfo"] = ("Additional Infrastructural Information", "Infrastructure", "Describe any additional infrastructure not covered by the structured tables."),
        //        ["Disclosure.LibraryInfo"] = ("Library Facilities", "Library", "Describe library holdings, e-resources, reading rooms, and facilities."),
        //        ["Disclosure.BestPractices"] = ("Best Practices Adopted (if any)", "Best Practices", "Describe any best practices followed by the institute."),
        //    };

        public DisclosureNarrativeController(ApplicationDbContext context)
            => _context = context;

        // ════════════════════════════════════════════════════
        // GET /DisclosureNarrative/Index
        // Shows ALL 19 fixed sections grouped by category
        // ════════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Mandatory Disclosure — Narrative Sections";

            var list = new List<NarrativeIndexVM>();

            // ── DB-backed governance sections ──────────────
            var dbRows = await _context.DisclosureNarratives
                .Where(n => !n.IsDeleted)
                .ToListAsync();

            foreach (var key in DisclosureSectionKeys.AllKeys)
            {
                // Skip SiteSettings-backed keys from DB loop
                //if (_settingsKeys.ContainsKey(key)) continue;

                var rows = dbRows.Where(n => n.SectionKey == key).ToList();
                var best = rows
                    .OrderByDescending(n =>
                        !string.IsNullOrWhiteSpace(n.HtmlContent) ? 1 : 0)
                    .ThenBy(n => n.DisplayOrder)
                    .FirstOrDefault();

                list.Add(new NarrativeIndexVM
                {
                    SectionKey = key,
                    SectionTitle = best?.SectionTitle
                        ?? DisclosureSectionKeys.DefaultTitles[key],
                    SectionGroup = GetGroup(key),
                    HasContent = best != null
                        && !string.IsNullOrWhiteSpace(best.HtmlContent),
                    IsVisible = best?.IsVisible ?? true,
                    IsDbSection = true,
                    LastUpdated = best?.LastUpdated,
                    UpdatedBy = best?.UpdatedBy
                });
            }

            // ── SiteSettings-backed sections ───────────────
            //var settings = await _context.SiteSettings
            //    .Where(s => _settingsKeys.Keys.Contains(s.Key))
            //    .ToListAsync();

            //foreach (var (key, meta) in _settingsKeys)
            //{
            //    var setting = settings.FirstOrDefault(s => s.Key == key);
            //    list.Add(new NarrativeIndexVM
            //    {
            //        SectionKey = key,
            //        SectionTitle = meta.Title,
            //        SectionGroup = meta.Group,
            //        HasContent = setting != null
            //            && !string.IsNullOrWhiteSpace(setting.Value),
            //        IsVisible = true,
            //        IsDbSection = false,
            //        LastUpdated = null,
            //        UpdatedBy = null
            //    });
            //}

            // Group order
            var groupOrder = new[]
            {
                "About", "Governance", "Programs", "Placement",
                "Fee", "Admission", "Infrastructure", "Library",
                "Best Practices"
            };

            var grouped = list
                .OrderBy(x => Array.IndexOf(groupOrder, x.SectionGroup))
                .ThenBy(x => x.SectionTitle)
                .GroupBy(x => x.SectionGroup)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Grouped = grouped;
            return View();
        }

        // ════════════════════════════════════════════════════
        // GET /DisclosureNarrative/Edit?key=Governance
        // ════════════════════════════════════════════════════
        public async Task<IActionResult> Edit(string key)
        {
            //if (!DisclosureSectionKeys.AllKeys.Contains(key)
            //    && !_settingsKeys.ContainsKey(key))
            //    return NotFound("Invalid section key.");

            ViewData["Title"] = "Edit Narrative Section";

            // SiteSettings-backed
            //if (_settingsKeys.TryGetValue(key, out var meta))
            //{
            //    var setting = await _context.SiteSettings
            //        .FirstOrDefaultAsync(s => s.Key == key);

            //    return View(new NarrativeEditVM
            //    {
            //        SectionKey = key,
            //        SectionTitle = meta.Title,
            //        SectionGroup = meta.Group,
            //        HtmlContent = setting?.Value,
            //        IsVisible = true,
            //        IsDbSection = false,
            //        HelpText = meta.Help
            //    });
            //}

            // DB-backed
            var rows = await _context.DisclosureNarratives
                .Where(n => n.SectionKey == key && !n.IsDeleted)
                .OrderByDescending(n =>
                    string.IsNullOrWhiteSpace(n.HtmlContent) ? 0 : 1)
                .ThenBy(n => n.DisplayOrder)
                .ToListAsync();

            // If none exist, create a placeholder
            if (!rows.Any())
            {
                var newRow = new DisclosureNarrative
                {
                    SectionKey = key,
                    SectionTitle = DisclosureSectionKeys.DefaultTitles[key],
                    HtmlContent = null,
                    DisplayOrder = Array.IndexOf(DisclosureSectionKeys.AllKeys, key),
                    IsVisible = true
                };
                _context.DisclosureNarratives.Add(newRow);
                await _context.SaveChangesAsync();
                rows = new List<DisclosureNarrative> { newRow };
            }

            // Deduplicate: keep best, soft-delete others
            var best = rows.First();
            foreach (var dup in rows.Skip(1))
            {
                dup.IsDeleted = true;
            }
            if (rows.Count > 1) await _context.SaveChangesAsync();

            return View(new NarrativeEditVM
            {
                SectionKey = best.SectionKey,
                SectionTitle = best.SectionTitle,
                SectionGroup = GetGroup(key),
                HtmlContent = best.HtmlContent,
                IsVisible = best.IsVisible,
                IsDbSection = true,
                HelpText = GetHelp(key)
            });
        }

        // ════════════════════════════════════════════════════
        // POST /DisclosureNarrative/Edit
        // ════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NarrativeEditVM model)
        {
            ViewData["Title"] = "Edit Narrative Section";

            if (!ModelState.IsValid)
                return View(model);

            string userName = User.Identity?.Name ?? "Admin";

            // SiteSettings-backed
           // if (!model.IsDbSection)
            //{
                //var setting = await _context.SiteSettings
                //    .FirstOrDefaultAsync(s => s.Key == model.SectionKey);

                //if (setting == null)
                //{
                //    _context.SiteSettings.Add(new SiteSetting
                //    {
                //        Key = model.SectionKey,
                //        Value = model.HtmlContent ?? "",
                //        Group = "Disclosure"
                //    });
                //}
                //else
                //{
                //    setting.Value = model.HtmlContent ?? "";
                //}

                //await _context.SaveChangesAsync();
                //TempData["Success"] = $"'{model.SectionTitle}' saved.";
                //return RedirectToAction(nameof(Index));
            //}

            // DB-backed — find single best row
            var rows = await _context.DisclosureNarratives
                .Where(n => n.SectionKey == model.SectionKey && !n.IsDeleted)
                .ToListAsync();

            if (!rows.Any())
            {
                _context.DisclosureNarratives.Add(new DisclosureNarrative
                {
                    SectionKey = model.SectionKey,
                    SectionTitle = model.SectionTitle,
                    HtmlContent = model.HtmlContent,
                    IsVisible = model.IsVisible,
                    DisplayOrder = Array.IndexOf(
                        DisclosureSectionKeys.AllKeys, model.SectionKey),
                    LastUpdated = DateTime.Now,
                    UpdatedBy = userName
                });
            }
            else
            {
                var best = rows
                    .OrderByDescending(n =>
                        !string.IsNullOrWhiteSpace(n.HtmlContent) ? 1 : 0)
                    .First();

                best.HtmlContent = model.HtmlContent;
                best.IsVisible = model.IsVisible;
                best.LastUpdated = DateTime.Now;
                best.UpdatedBy = userName;

                // Soft-delete any duplicates
                foreach (var dup in rows.Where(n => n.Id != best.Id))
                    dup.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{model.SectionTitle}' saved.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────────
        private static string GetGroup(string key) => key switch
        {
            DisclosureSectionKeys.Governance or
            DisclosureSectionKeys.AcademicAdvisoryBody or
            DisclosureSectionKeys.BoardMeetings or
            DisclosureSectionKeys.OrganizationalChart or
            DisclosureSectionKeys.FacultyStudentInvolvement or
            DisclosureSectionKeys.GovernanceMechanism or
            DisclosureSectionKeys.StudentFeedback or
            DisclosureSectionKeys.GrievanceRedressal => "Governance",
            DisclosureSectionKeys.AccreditationStatus => "Programs",
            DisclosureSectionKeys.PlacementFacilities or
            DisclosureSectionKeys.ForeignCollaboration => "Placement",
            DisclosureSectionKeys.FeeStructure => "Fee",
            DisclosureSectionKeys.AdmissionProcess or
            DisclosureSectionKeys.CriteriaWeightages or
            DisclosureSectionKeys.ApplicantList or
            DisclosureSectionKeys.ManagementSeatsResult => "Admission",
            DisclosureSectionKeys.InfrastructureInfo => "Infrastructure",
            DisclosureSectionKeys.LibraryInfo => "Library",
            DisclosureSectionKeys.BestPractices => "Best Practices",
            _ => "Other"
        };

        private static string GetHelp(string key) => key switch
        {
            DisclosureSectionKeys.Governance
                => "Describe how the institute is governed (State/Central Govt., etc.).",
            DisclosureSectionKeys.AcademicAdvisoryBody
                => "List members of the Academic Advisory Body and their roles.",
            DisclosureSectionKeys.BoardMeetings
                => "Add details about governing board meetings, decisions, and agenda.",
            DisclosureSectionKeys.OrganizationalChart
                => "Describe the organizational hierarchy (can embed an image or table).",
            DisclosureSectionKeys.FacultyStudentInvolvement
                => "Describe how faculty and students are involved in academic affairs.",
            DisclosureSectionKeys.GovernanceMechanism
                => "Describe mechanisms and norms for good governance.",
            DisclosureSectionKeys.StudentFeedback
                => "Describe the student feedback mechanism on institutional governance.",
            DisclosureSectionKeys.GrievanceRedressal
                => "Describe the grievance redressal mechanism for students and staff.",
            DisclosureSectionKeys.AccreditationStatus
                => "Add narrative about NAAC grade, NBA status, and other accreditations.",
            DisclosureSectionKeys.PlacementFacilities
                => "Describe placement cell infrastructure, tie-ups, activities.",
            DisclosureSectionKeys.ForeignCollaboration
                => "List MoUs or collaborations with foreign institutions (or write NA).",
            _ => "Add content for this section using the editor below."
        };
    }
}
