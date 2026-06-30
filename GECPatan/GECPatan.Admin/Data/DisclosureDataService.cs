using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.EntityFrameworkCore;
using NuGet.Configuration;

namespace GECPatan.Admin.Services
{
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

            var settings = await _context.SiteSettings.ToListAsync();
            string? Cfg(string key) =>
                settings.FirstOrDefault(s => s.Key == key)?.Value;
            string? Narrative(string key) =>
                _context.DisclosureNarratives
                    .Where(n => n.SectionKey == key && !n.IsDeleted)
                    .OrderByDescending(n => !string.IsNullOrWhiteSpace(n.HtmlContent))
                    .ThenBy(n => n.DisplayOrder)
                    .Select(n => n.HtmlContent)
                    .FirstOrDefault();

            // ── SECTION 1: About Institute ──────────────────
            vm.AboutInstitute = Narrative("AboutInstitute");

            // ── SECTION 2: Institute Info ───────────────────
            vm.Institute = new InstituteInfoVM
            {
                Name = Cfg("Institute.Name") ?? "Government Engineering College, Patan",
                Address = Cfg("Contact.Address"),
                Phone = Cfg("Contact.Phone"),
                Email = Cfg("Contact.Email"),
                EstablishedYear = Cfg("College.EstablishedYear"),
                AffiliatedTo = Cfg("Institute.AffiliatedTo")
                                    ?? Cfg("Institute.University")
                                    ?? "Gujarat Technological University",
                ApprovedBy = Cfg("Institute.ApprovedBy") ?? "AICTE"
            };

            // ── SECTION 3: Governance Narratives ────────────
            var govKeys = new[]
            {
                DisclosureSectionKeys.Governance,
                DisclosureSectionKeys.AcademicAdvisoryBody,
                DisclosureSectionKeys.BoardMeetings,
                DisclosureSectionKeys.OrganizationalChart,
                DisclosureSectionKeys.FacultyStudentInvolvement,
                DisclosureSectionKeys.GovernanceMechanism,
                DisclosureSectionKeys.StudentFeedback,
                DisclosureSectionKeys.GrievanceRedressal
            };

            foreach (var key in govKeys)
                await AddNarrative(vm.GovernanceNarratives, key);

            // ── SECTION 4: Programs ─────────────────────────
            vm.StudentToFacultyRatioHtml = Narrative("StudentToFacultyRatio");
            vm.AccreditationStatusHtml = Narrative("AccreditationStatus");

            vm.NBAAccreditations = await _context.NBAAccreditations
                .Where(n => n.IsVisible)
                .OrderBy(n => n.DisplayOrder)
                .Select(n => new NBAAccreditationVM
                {
                    ProgramName = n.ProgramName,
                    AccreditedBy = n.AccreditedBy,
                    ValidFrom = n.ValidFrom,
                    ValidTo = n.ValidTo,
                    Status = n.Status
                }).ToListAsync();

            var depts = await _context.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            foreach (var dept in depts)
            {
                var intake = await _context.ProgramIntakes
                    .Where(p => p.DeptId == dept.DeptId && p.IsVisible)
                    .OrderByDescending(p => p.IntakeYear)
                    .FirstOrDefaultAsync();

                if (intake == null || intake.Intake == 0) continue;

                vm.Programs.Add(new DisclosureDeptProgramVM
                {
                    DeptName = dept.Name,
                    ShortCode = dept.ShortCode,
                    Intake = intake.Intake,
                    IntakeYear = intake.IntakeYear
                });
            }

            // ── SECTION 5: Cutoffs per dept ─────────────────
            foreach (var dept in depts)
            {
                var intake = await _context.ProgramIntakes
                    .Where(p => p.DeptId == dept.DeptId && p.IsVisible)
                    .OrderByDescending(p => p.IntakeYear)
                    .FirstOrDefaultAsync();
                if (intake == null || intake.Intake == 0) continue;

                var cutoffs = await _context.CutoffRecords
                    .Where(c => c.DeptId == dept.DeptId && c.IsVisible)
                    .OrderByDescending(c => c.AcademicYear)
                    .ToListAsync();

                vm.DeptCutoffs.Add(new DisclosureDeptVM
                {
                    DeptId = dept.DeptId ?? 0,
                    Name = dept.Name,
                    ShortCode = dept.ShortCode,
                    CurrentIntake = intake.Intake,
                    CurrentIntakeYear = intake.IntakeYear,
                    CutoffHistory = cutoffs.Select(c => new DisclosureCutoffRowVM
                    {
                        AcademicYear = c.AcademicYear,
                        General = c.GeneralRank?.ToString() ?? "—",
                        SEBC = c.SEBCRank?.ToString() ?? "—",
                        SC = c.SCRank?.ToString() ?? "—",
                        ST = c.STRank?.ToString() ?? "—",
                        EWS = c.EWSRank?.ToString() ?? "—"
                    }).ToList()
                });
            }

            // ── SECTION 6: Placement ─────────────────────────
            vm.PlacementFacilitiesHtml = Narrative("PlacementFacilities");
            vm.ForeignCollaborationHtml = Narrative("ForeignCollaboration");

            vm.PlacementData = await _context.DisclosurePlacements
                .Where(p => p.IsVisible)
                .OrderByDescending(p => p.AcademicYear)
                .Select(p => new DisclosurePlacementRowVM
                {
                    Id = p.Id,
                    DeptId = p.DeptId,
                    DepartmentName = p.Department.Name,
                    AcademicYear = p.AcademicYear,
                    NoOfCompanies = p.NoOfCompanies,
                    TotalPlaced = p.TotalPlaced,
                    MaximumSalary = p.MaximumSalary,
                    MinimumSalary = p.MinimumSalary,
                    DisplayOrder = p.DisplayOrder,
                    IsVisible = p.IsVisible

                }).ToListAsync();

            // ── SECTION 7: Faculty ───────────────────────────
            var principal = await _context.Principals
                .Include(p => p.Qualifications)
                .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted);

            if (principal != null)
            {
                vm.Principal = new DisclosurePrincipalVM
                {
                    Name = principal.Name,
                    Designation = principal.Designation,
                    Email = principal.Email,
                    Contact = principal.Contact,
                    Qualifications = principal.Qualifications
                        .Select(q => q.Degree).ToList()
                };
            }

            // Dept-wise faculty
            foreach (var dept in depts)
            {
                var intake = await _context.ProgramIntakes
                    .Where(p => p.DeptId == dept.DeptId && p.IsVisible)
                    .OrderByDescending(p => p.IntakeYear)
                    .FirstOrDefaultAsync();
            //    if (intake == null || intake.Intake == 0) continue;

                var facultyList = await _context.Faculties
                    .Where(f => f.DeptId == dept.DeptId
                             && f.IsActive && f.IsTeaching)
                    .OrderBy(f => f.SeniorityOrder)
                    .ToListAsync();

                var ids = facultyList.Select(f => f.FacultyId).ToList();

                // Load all sub-tables
                var quals = await _context.FacultyQualifications
                    .Where(q => ids.Contains(q.FacultyId)).ToListAsync();
                var exps = await _context.FacultyExperiences
                    .Where(e => ids.Contains(e.FacultyId)).ToListAsync();
                var subjs = await _context.FacultySubjects
                    .Where(s => ids.Contains(s.FacultyId)).ToListAsync();
                var guidance = await _context.FacultyResearchGuidances
                    .Where(g => ids.Contains(g.FacultyId)).ToListAsync();
                var pubs = await _context.FacultyPublications
                    .Where(p => ids.Contains(p.FacultyId)).ToListAsync();
                var books = await _context.FacultyBookPublications
                    .Where(b => ids.Contains(b.FacultyId)).ToListAsync();
                var cons = await _context.FacultyConsultancies
                    .Where(c => ids.Contains(c.FacultyId)).ToListAsync();
                var patents = await _context.FacultyPatents
                    .Where(p => ids.Contains(p.FacultyId)).ToListAsync();
                var trainings = await _context.FacultyTrainings
                    .Where(t => ids.Contains(t.FacultyId)).ToListAsync();
                var memberships = await _context.FacultyProfessionalMemberships
                    .Where(m => ids.Contains(m.FacultyId)).ToListAsync();
                var approvals = await _context.FacultyApprovalInfos
                    .Where(a => ids.Contains(a.FacultyId)).ToListAsync();
                var personalDetails = await _context.PersonalDetails
                    .Where(p => ids.Contains(p.FacultyId)).ToListAsync();

                var deptVM = new DisclosureDeptFacultyVM
                {
                    DeptId = dept.DeptId ?? 0,
                    DeptName = dept.Name
                };
                deptVM.Turnover = await _context.FacultyTurnoverRecords
                .Where(t => t.DeptId == dept.DeptId && t.IsVisible)
                .OrderByDescending(t => t.AcademicYear)
                .Take(3)
                .Select(t => new FacultyTurnoverRowVM
                {
                    AcademicYear = t.AcademicYear,
                    NonTeachingJoin = t.NonTeachingJoin,
                    TeachingJoin = t.TeachingJoin,
                    NonTeachingLeft = t.NonTeachingLeft,
                    TeachingLeft = t.TeachingLeft
                })
                .ToListAsync();
                int sr = 1;
                foreach (var f in facultyList)
                {
                    var appr = approvals.FirstOrDefault(a => a.FacultyId == f.FacultyId);
                    var pd = personalDetails.FirstOrDefault(p => p.FacultyId == f.FacultyId);

                    // Summary table row
                    deptVM.SummaryTable.Add(new FacultySummaryRowVM
                    {
                        SrNo = sr++,
                        Name = f.Name,
                        Post = f.Designation,
                        ApprovalStatus = appr?.ApprovalStatus ?? "Approved",
                        ApprovalLetterNumber = f.LetterNumber
                    });

                    // Full profile
                    var fQuals = quals.Where(q => q.FacultyId == f.FacultyId).ToList();
                    var fExps = exps.Where(e => e.FacultyId == f.FacultyId)
                        .OrderBy(e => e.FromDate).ToList();
                    var fSubjs = subjs.Where(s => s.FacultyId == f.FacultyId).ToList();
                    var fGuidance = guidance.Where(g => g.FacultyId == f.FacultyId).ToList();
                    var fPubs = pubs.Where(p => p.FacultyId == f.FacultyId).ToList();
                    var fBooks = books.Where(b => b.FacultyId == f.FacultyId).ToList();
                    var fCons = cons.Where(c => c.FacultyId == f.FacultyId).ToList();
                    var fPatents = patents.Where(p => p.FacultyId == f.FacultyId).ToList();
                    var fTrains = trainings.Where(t => t.FacultyId == f.FacultyId).ToList();
                    var fMembers = memberships.Where(m => m.FacultyId == f.FacultyId).ToList();

                    int teachingYrs = (int)((DateTime.Now - f.DateOfJoining).TotalDays / 365.25);

                    deptVM.Faculty.Add(new DisclosureFacultyDetailVM
                    {
                        FacultyId = f.FacultyId,
                        Name = f.Name,
                        Designation = f.Designation,
                        Email = pd?.Email,
                        DateOfBirth = pd?.DateOfBirth.HasValue == true
                            ? pd.DateOfBirth.Value.ToString("dd MMM yyyy") : null,
                        Qualifications = string.Join(", ", fQuals
                            .OrderByDescending(q => q.Year)
                            .Select(q => q.Degree)),
                        Website = f.Website,
                        AreaOfInterest = f.AreaOfInterest,
                        ImagePath = f.ImagePath,
                        TeachingYears = teachingYrs,
                        Experiences = fExps.Select(e => new ExperienceRowVM
                        {
                            Organization = e.Organization,
                            Period = $"{e.FromDate?.ToString("MMM yyyy") ?? "—"} to {(e.ToDate.HasValue ? e.ToDate.Value.ToString("MMM yyyy") : "till date")}",
                            Duration = FormatDuration(e.FromDate, e.ToDate)
                        }).ToList(),
                        UGSubjects = fSubjs.Where(s => s.Level == "UG")
                            .Select(s => s.SubjectName).ToList(),
                        PGSubjects = fSubjs.Where(s => s.Level == "PG")
                            .Select(s => s.SubjectName).ToList(),
                        MastersOngoing = fGuidance.FirstOrDefault(g => g.Level == "Masters")?.Ongoing ?? 0,
                        MastersCompleted = fGuidance.FirstOrDefault(g => g.Level == "Masters")?.Completed ?? 0,
                        PhDOngoing = fGuidance.FirstOrDefault(g => g.Level == "PhD")?.Ongoing ?? 0,
                        PhDCompleted = fGuidance.FirstOrDefault(g => g.Level == "PhD")?.Completed ?? 0,
                        Publications = fPubs.OrderBy(p => p.SrNo).Select(p => new PublicationRowVM
                        {
                            SrNo = p.SrNo,
                            Title = p.Title,
                            Type = p.Type,
                            Journal = p.JournalName,
                            Year = p.Year,
                            CoAuthors = p.CoAuthors
                        }).ToList(),
                        ConsultancyCount = fCons.Count,
                        Patents = fPatents.Select(p => new PatentRowVM
                        {
                            Title = p.Title,
                            ApplicationNo = p.ApplicationNo,
                            Status = p.Status
                        }).ToList(),
                        Books = fBooks.OrderBy(b => b.SrNo).Select(b => new BookRowVM
                        {
                            SrNo = b.SrNo,
                            Title = b.Title,
                            Publisher = b.Publisher,
                            Year = b.Year
                        }).ToList(),
                        Trainings = fTrains
                            .Where(t => t.TrainingType != "Seminar" && t.TrainingType != "Workshop")
                            .Select(t => t.Title).ToList(),
                        Seminars = fTrains
                            .Where(t => t.TrainingType == "Seminar" || t.TrainingType == "Workshop")
                            .Select(t => t.Title).ToList(),
                        Memberships = fMembers.Select(m => new MembershipRowVM
                        {
                            Community = m.Community,
                            MembershipType = m.MembershipType
                        }).ToList()
                    });
                }

                vm.DeptFaculty.Add(deptVM);
            }

            // ── SECTION 8: Fee ───────────────────────────────
            vm.FeeStructureHtml = Narrative("FeeStructure");

            // ── SECTION 9: Admission ─────────────────────────
            vm.AdmissionProcessHtml = Narrative("AdmissionProcess");
            vm.CriteriaWeightagesHtml = Narrative("CriteriaWeightages");
            vm.ApplicantListHtml = Narrative("ApplicantList");
            vm.ManagementSeatsHtml = Narrative("ManagementSeatsResult");

            // ── SECTION 10: Infrastructure ───────────────────
            var allInfra = await _context.InfrastructureRecords
                .Include(i => i.Department)
                .Where(i => i.IsVisible)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();

            vm.Classrooms = MapInfra(allInfra, "Classroom");
            vm.TutorialRooms = MapInfra(allInfra, "Tutorial Room");
            vm.Laboratories = MapInfra(allInfra, "Laboratory");
            vm.DrawingHalls = MapInfra(allInfra, "Drawing Hall");
            vm.OtherInfra = allInfra
                .Where(i => !new[]{"Classroom","Tutorial Room",
                                   "Laboratory","Drawing Hall"}
                    .Contains(i.RoomType))
                .Select(i => new InfrastructureRowVM
                {
                    RoomType = i.RoomType,
                    AreaSqm = i.AreaSqm,
                    BuildingName = i.BuildingName ?? "—",
                    DeptName = i.Department?.Name ?? "Institute-wide"
                }).ToList();

            vm.InfrastructureInfoHtml = Narrative("InfrastructureInfo");

            // ── SECTION 11: Library ──────────────────────────
            vm.LibraryInfoHtml = Narrative("LibraryInfo");

            // ── SECTION 12: Dept Equipment ───────────────────
            var equipments = await _context.DepartmentEquipments.ToListAsync();
            foreach (var dept in depts)
            {
                var intake = await _context.ProgramIntakes
                    .Where(p => p.DeptId == dept.DeptId && p.IsVisible)
                    .OrderByDescending(p => p.IntakeYear)
                    .FirstOrDefaultAsync();
                if (intake == null || intake.Intake == 0) continue;

                var eq = equipments.FirstOrDefault(e => e.DeptId == dept.DeptId);
                vm.DeptEquipments.Add(new DeptEquipmentVM
                {
                    DeptName = dept.Name,
                    EquipmentHtml = eq?.EquipmentHtml
                });
            }

            // ── SECTION 13: Best Practices ───────────────────
            vm.BestPracticesHtml = Narrative("BestPractices");

            // ── Warnings ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(vm.AboutInstitute))
                vm.EmptyNarrativeWarnings.Add("About Institute section is empty.");
            if (!vm.Programs.Any())
                vm.EmptyNarrativeWarnings.Add("No programs found.");
            if (!vm.PlacementData.Any())
                vm.EmptyNarrativeWarnings.Add("No placement data added.");
            if (string.IsNullOrWhiteSpace(vm.FeeStructureHtml))
                vm.EmptyNarrativeWarnings.Add("Fee Structure section is empty.");

            return vm;
        }

        // ── HELPERS ───────────────────────────────────────────
        private async Task AddNarrative(
            List<NarrativeSectionVM> list, string key)
        {
            var rows = await _context.DisclosureNarratives
                .Where(n => n.SectionKey == key && !n.IsDeleted)
                .ToListAsync();

            var best = rows
                .OrderByDescending(n =>
                    !string.IsNullOrWhiteSpace(n.HtmlContent) ? 1 : 0)
                .ThenBy(n => n.DisplayOrder)
                .FirstOrDefault();

            list.Add(new NarrativeSectionVM
            {
                SectionKey = key,
                SectionTitle = best?.SectionTitle
                    ?? DisclosureSectionKeys.DefaultTitles[key],
                HtmlContent = best?.HtmlContent,
                DisplayOrder = list.Count
            });
        }

        private static List<InfrastructureRowVM> MapInfra(
            List<GECPatan.Core.Models.Domain.InfrastructureRecord> all,
            string type)
        {
            return all.Where(i => i.RoomType == type)
                .Select(i => new InfrastructureRowVM
                {
                    RoomType = i.RoomType,
                    AreaSqm = i.AreaSqm,
                    BuildingName = i.BuildingName ?? "—",
                    DeptName = i.Department?.Name ?? "Institute-wide"
                }).ToList();
        }

        private static string FormatDuration(DateTime? from, DateTime? to)
        {
            if (!from.HasValue) return "—";
            var end = to ?? DateTime.Now;
            var days = (end - from.Value).TotalDays;
            int yrs = (int)(days / 365.25);
            int mos = (int)((days % 365.25) / 30.44);
            return $"{yrs} Yrs {mos} Months";
        }
    }
}
