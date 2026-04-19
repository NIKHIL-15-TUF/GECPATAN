using GECPatan.Admin.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // DEPARTMENT
        public DbSet<Department> Departments { get; set; }
        public DbSet<DepartmentVision> DepartmentVisions { get; set; }
        public DbSet<DepartmentMission> DepartmentMissions { get; set; }
        public DbSet<DepartmentPEO> DepartmentPEOs { get; set; }
        public DbSet<DepartmentPSO> DepartmentPSOs { get; set; }
        public DbSet<DepartmentBannerImage> DepartmentImages { get; set; }

        // ── TIMETABLE ─────────────────────────────────────
        public DbSet<Timetable> Timetables { get; set; }

        // ── LABS ──────────────────────────────────────────
        public DbSet<Lab> Labs { get; set; }
        public DbSet<LabImage> LabImages { get; set; }
        
        // FACULTY
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<PersonalDetail> PersonalDetails { get; set; }
        public DbSet<FacultyQualification> FacultyQualifications { get; set; }
        public DbSet<FacultyExperience> FacultyExperiences { get; set; }
        public DbSet<FacultyTraining> FacultyTrainings { get; set; }
        public DbSet<FacultyPublication> FacultyPublications { get; set; }

        // CAMPUS COMMITTEES
        // ── COMMITTEES ────────────────────────────────────
        public DbSet<CampusCommittee> CampusCommittees { get; set; }
        public DbSet<CommitteeVision> CommitteeVisions { get; set; }
        public DbSet<CommitteeMission> CommitteeMissions { get; set; }
        public DbSet<CommitteeObjective> CommitteeObjectives { get; set; }
        public DbSet<CommitteeSubObjective> CommitteeSubObjectives { get; set; }
        public DbSet<CommitteeMember> CommitteeMembers { get; set; }
        public DbSet<AdditionalMemberGroup> AdditionalMemberGroups { get; set; }
        public DbSet<AdditionalMemberDetail> AdditionalMemberDetails { get; set; }

        // ACTIVITIES & ACHIEVEMENTS
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityImage> ActivityImages { get; set; }
        public DbSet<ActivityFile> ActivityFiles { get; set; }
        public DbSet<Achievement> Achievements { get; set; }

        // DYNAMIC SECTIONS
        public DbSet<DynamicSection> DynamicSections { get; set; }
        public DbSet<DynamicSectionFile> DynamicSectionFiles { get; set; }

        // HOME PAGE
        public DbSet<Slider> Sliders { get; set; }
        public DbSet<Marquee> Marquees { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<TopRecruiter> TopRecruiters { get; set; }

        // NEWS
        public DbSet<NewsItem> NewsItems { get; set; }
        public DbSet<NewsItemImage> NewsItemImages { get; set; }
        public DbSet<NewsItemFile> NewsItemFiles { get; set; }
        public DbSet<NewsLetter> NewsLetters { get; set; }

        // DOCUMENTS
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<DocumentYearSection> DocumentYearSections { get; set; }
        public DbSet<DocumentFile> DocumentFiles { get; set; }
        public DbSet<TenderCategory> TenderCategories { get; set; }
        public DbSet<TenderDocument> TenderDocuments { get; set; }
        public DbSet<ImportantDocument> ImportantDocuments { get; set; }
        public DbSet<MoUDocument> MoUDocuments { get; set; }

        // ACADEMICS
        public DbSet<AcademicCalendar> AcademicCalendars { get; set; }
        public DbSet<SSIPDocument> SSIPDocuments { get; set; }
        public DbSet<ResearchGrant> ResearchGrants { get; set; }
        public DbSet<ProgramIntake> ProgramIntakes { get; set; }

        // PLACEMENT
        public DbSet<PlacementStatistic> PlacementStatistics { get; set; }
        public DbSet<PlacementTeamMember> PlacementTeamMembers { get; set; }

        // STUDENT & CLUBS
        public DbSet<StudentClub> StudentClubs { get; set; }
        public DbSet<ClubImage> ClubImages { get; set; }
        public DbSet<ClubMember> ClubMembers { get; set; }
        public DbSet<ClubObjective> ClubObjectives { get; set; }

        // SITE CONTENT
        public DbSet<Alumni> Alumni { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<AboutUs> AboutUs { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }
        public DbSet<ContentPage> ContentPages { get; set; }

        // ── AUDIT LOG ─────────────────────────────────────
        public DbSet<AuditLog> AuditLogs { get; set; }

        // MODEL CREATING
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── GLOBAL SOFT DELETE FILTERS ──────────────────
            modelBuilder.Entity<Department>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Faculty>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<FacultyQualification>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<FacultyExperience>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<FacultyTraining>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<FacultyPublication>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CampusCommittee>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CommitteeVision>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CommitteeMission>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CommitteeObjective>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CommitteeSubObjective>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<CommitteeMember>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Activity>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Achievement>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<DynamicSection>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<NewsItem>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Slider>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Marquee>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Testimonial>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<TopRecruiter>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<StudentClub>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<Alumni>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<ProgramIntake>()
                .HasIndex(p => new { p.DeptId, p.IntakeYear })
                .IsUnique();
            modelBuilder.Entity<ContentPage>()
                .HasIndex(p => p.Slug)
                .IsUnique();
            // ── RELATIONSHIPS ────────────────────────────────

            // Faculty → Department
            modelBuilder.Entity<Faculty>()
                .HasOne(f => f.Department)
                .WithMany(d => d.Faculties)
                .HasForeignKey(f => f.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser → Department
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany()
                .HasForeignKey(u => u.DeptId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // ApplicationUser → Faculty
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Faculty)
                .WithMany()
                .HasForeignKey(u => u.FacultyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // SiteSetting → unique key
            modelBuilder.Entity<SiteSetting>()
                .HasIndex(s => s.Key)
                .IsUnique();

        }

        // =============================================
        // SAVE CHANGES — AUTO AUDIT
        // =============================================
        public override int SaveChanges()
        {
            ApplyAuditInfo();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            ApplyAuditInfo();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditInfo()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDate = DateTime.Now;
                    entry.Entity.CreatedDateInt =
                        DateTimeOffset.Now.ToUnixTimeSeconds();
                    entry.Entity.IsDeleted = false;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = DateTime.Now;
                    entry.Entity.UpdatedDateInt =
                        DateTimeOffset.Now.ToUnixTimeSeconds();
                }
            }
        }
    }
}