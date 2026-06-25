using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Services
{
    // Single source of truth for Mandatory Disclosure data.
    // Both DOCX and PDF generators call BuildAsync() and use
    // the SAME DisclosureDataVM — guarantees the two outputs
    // never drift apart from each other.
    public class DisclosureDataService
    {
        private readonly ApplicationDbContext _context;

        public DisclosureDataService(ApplicationDbContext context)
            => _context = context;

        public async Task<DisclosureDataVM> BuildAsync(string academicYear)
        {
            var vm = new DisclosureDataVM
            {
                AcademicYear = academicYear,
                GeneratedOn = DateTime.Now
            };

            // ── INSTITUTE INFO ──────────────────────────────
            var settings = await _context.SiteSettings.ToListAsync();
            string? Cfg(string key) =>
                settings.FirstOrDefault(s => s.Key == key)?.Value;

            vm.Institute = new InstituteInfoVM
            {
                Name = Cfg("Institute.Name") ?? "GEC Patan",
                Address = Cfg("Contact.Address"),
                Phone = Cfg("Contact.Phone"),
                Email = Cfg("Contact.Email"),
                EstablishedYear = Cfg("College.EstablishedYear"),
                AffiliatedTo = Cfg("Institute.AffiliatedTo"),
                ApprovedBy = Cfg("Institute.ApprovedBy")
            };

            // ── PRINCIPAL ────────────────────────────────────
            var principal = await _context.Principals
                .Include(p => p.Qualifications)
                .FirstOrDefaultAsync(p => p.IsActive);

            if (principal != null)
            {
                vm.Principal = new PrincipalSummaryVM
                {
                    Name = principal.Name,
                    Designation = principal.Designation,
                    Email = principal.Email,
                    Contact = principal.Contact,
                    Qualifications = principal.Qualifications
                        .Select(q => q.Degree).ToList()
                };
            }

            // ── DEPARTMENTS + FACULTY + CUTOFFS ─────────────
            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            foreach (var dept in depts)
            {
                var deptVM = new DisclosureDeptVM
                {
                    DeptId = dept.DeptId ?? 0,
                    Name = dept.Name,
                    ShortCode = dept.ShortCode
                };

                var latestIntake = await _context.ProgramIntakes
                    .Where(p => p.DeptId == dept.DeptId)
                    .OrderByDescending(p => p.IntakeYear)
                    .FirstOrDefaultAsync();

                deptVM.CurrentIntake = latestIntake?.Intake ?? 0;
                deptVM.CurrentIntakeYear = latestIntake?.IntakeYear ?? 0;

                // Cutoff history (all years on record)
                var cutoffs = await _context.CutoffRecords
                    .Where(c => c.DeptId == dept.DeptId && c.IsVisible)
                    .OrderByDescending(c => c.AcademicYear)
                    .ToListAsync();

                deptVM.CutoffHistory = cutoffs.Select(c => new DisclosureCutoffRowVM
                {
                    AcademicYear = c.AcademicYear,
                    General = c.GeneralRank?.ToString() ?? "—",
                    SEBC = c.SEBCRank?.ToString() ?? "—",
                    SC = c.SCRank?.ToString() ?? "—",
                    ST = c.STRank?.ToString() ?? "—",
                    EWS = c.EWSRank?.ToString() ?? "—"
                }).ToList();

                // Faculty (teaching staff only, per AICTE format)
                var faculty = await _context.Faculties
                    .Include(f => f.Qualifications)
                    .Include(f => f.Experiences)
                    .Include(f => f.Publications)
                    .Where(f => f.DeptId == dept.DeptId
                             && f.IsActive
                             && f.IsTeaching)
                    .OrderBy(f => f.SeniorityOrder)
                    .ToListAsync();

                deptVM.Faculty = faculty.Select(f =>
                {
                    var highestQual = f.Qualifications
                        .OrderByDescending(q => q.Year)
                        .Select(q => q.Degree)
                        .FirstOrDefault() ?? "—";

                    int expYears = f.Experiences
                        .Sum(e =>
                        {
                            var from = e.FromDate ?? DateTime.Now;
                            var to = e.ToDate ?? DateTime.Now;
                            return Math.Max(0, (to - from).Days / 365);
                        });

                    // Include current institute tenure too
                    expYears += Math.Max(0,
                        (DateTime.Now - f.DateOfJoining).Days / 365);

                    return new DisclosureFacultyVM
                    {
                        Name = f.Name,
                        Designation = f.Designation,
                        HighestQualification = highestQual,
                        DateOfJoining = f.DateOfJoining
                            .ToString("dd-MM-yyyy"),
                        ExperienceYears = expYears,
                        PublicationCount = f.Publications.Count
                    };
                }).ToList();

                vm.Departments.Add(deptVM);
            }

            // ── SCHOLARSHIPS ─────────────────────────────────
            var scholarships = await _context.ScholarshipRecords
                .Where(s => s.IsVisible)
                .OrderByDescending(s => s.AcademicYear)
                .ToListAsync();

            vm.Scholarships = scholarships.Select(s => new ScholarshipRowVM
            {
                SchemeName = s.SchemeName,
                AcademicYear = s.AcademicYear ?? "—",
                TotalApplications = s.TotalApplications
            }).ToList();

            // ── INFRASTRUCTURE ───────────────────────────────
            var infra = await _context.InfrastructureRecords
                .Include(i => i.Department)
                .Where(i => i.IsVisible)
                .OrderBy(i => i.RoomType)
                .ToListAsync();

            vm.Infrastructure = infra.Select(i => new InfrastructureRowVM
            {
                RoomType = i.RoomType,
                AreaSqm = i.AreaSqm,
                BuildingName = i.BuildingName ?? "—",
                DeptName = i.Department?.Name ?? "Institute-wide"
            }).ToList();

            // ── NARRATIVE SECTIONS (strict 15 keys) ─────────
            var narratives = await _context.DisclosureNarratives
                .Where(n => n.IsVisible)
                .OrderBy(n => n.DisplayOrder)
                .ToListAsync();

            vm.Narratives = narratives.Select(n => new NarrativeSectionVM
            {
                SectionKey = n.SectionKey,
                SectionTitle = n.SectionTitle,
                HtmlContent = n.HtmlContent,
                DisplayOrder = n.DisplayOrder
            }).ToList();

            // Validation warnings — sections with no content
            vm.EmptyNarrativeWarnings = narratives
                .Where(n => string.IsNullOrWhiteSpace(n.HtmlContent))
                .Select(n => n.SectionTitle)
                .ToList();

            return vm;
        }
    }
}