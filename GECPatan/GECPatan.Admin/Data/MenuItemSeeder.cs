using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Data
{
    public static class MenuItemSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Skip if already seeded
            if (await context.MenuItems.AnyAsync()) return;

            var items = new List<MenuItem>
            {
                // ── LEVEL 1 ──────────────────────────────
                new() { Id=1,  MenuText="Home",
                    ControllerName="Home", ActionName="Index",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { Id=2,  MenuText="Institute",
                    MenuType="Main", Position=2, IsVisible=true },

                new() { Id=3,  MenuText="Academics",
                    MenuType="Main", Position=3, IsVisible=true },

                new() { Id=4,  MenuText="Departments",
                    MenuType="Main", Position=4, IsVisible=true },

                new() { Id=6,  MenuText="R&D",
                    MenuType="Main", Position=5, IsVisible=true },

                new() { Id=7,  MenuText="Student Corner",
                    MenuType="Main", Position=6, IsVisible=true },

                new() { Id=8,  MenuText="Placement",
                    ControllerName="PlacementCell", ActionName="Index",
                    MenuType="Main", Position=7, IsVisible=true },

                new() { Id=99, MenuText="Administration",
                    MenuType="Main", Position=8, IsVisible=true },

                new() { Id=101,MenuText="Downloads",
                    ControllerName="Documents", ActionName="TableView",
                    DynamicId=100, DynamicType="Document",
                    MenuType="Main", Position=9, IsVisible=true },
 
                // ── INSTITUTE CHILDREN ────────────────────
                new() { ParentId=2, MenuText="About Us",
                    ControllerName="Institute", ActionName="AboutUs",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=2, MenuText="Mandatory Disclosure",
                    ControllerName="Documents", ActionName="Index",
                    DynamicId=30, DynamicType="Document",
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=2, MenuText="Governance",
                    Link="/ImpDocs/GEC_Organizational_Chart.pdf",
                    OpenInNewTab=true,
                    MenuType="Main", Position=3, IsVisible=true },

                new() { ParentId=2, MenuText="Newsletter",
                    ControllerName="Institute", ActionName="NewsLetter",
                    MenuType="Main", Position=4, IsVisible=true },

                new() { ParentId=2, MenuText="MoUs",
                    ControllerName="Documents", ActionName="TableView",
                    DynamicId=50, DynamicType="Document",
                    MenuType="Main", Position=5, IsVisible=true },

                new() { ParentId=2, MenuText="RTI",
                    ControllerName="CampusCommittee",
                    ActionName="CommitteePage",
                    DynamicId=6, DynamicType="Committee",
                    MenuType="Main", Position=6, IsVisible=true },

                new() { ParentId=2, MenuText="Information Booklet",
                    Link="/ImpDocs/Brochure_GECP.pdf",
                    OpenInNewTab=true,
                    MenuType="Main", Position=7, IsVisible=true },
 
                // ── DEPARTMENTS ───────────────────────────
                new() { ParentId=4, MenuText="Electronics & Communication",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=1, DynamicType="Department",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=4, MenuText="Computer Science & Engineering",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=2, DynamicType="Department",
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=4, MenuText="Electrical Engineering",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=3, DynamicType="Department",
                    MenuType="Main", Position=3, IsVisible=true },

                new() { ParentId=4, MenuText="Civil Engineering",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=4, DynamicType="Department",
                    MenuType="Main", Position=4, IsVisible=true },

                new() { ParentId=4, MenuText="Mechanical Engineering",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=5, DynamicType="Department",
                    MenuType="Main", Position=5, IsVisible=true },

                new() { ParentId=4, MenuText="Science & Humanities",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=6, DynamicType="Department",
                    MenuType="Main", Position=6, IsVisible=true },

                new() { ParentId=4, MenuText="Applied Mechanics",
                    ControllerName="Department",
                    ActionName="DepartmentDetails",
                    DynamicId=8, DynamicType="Department",
                    MenuType="Main", Position=7, IsVisible=true },
 
                // ── ACADEMICS ─────────────────────────────
                new() { ParentId=3, MenuText="Academic Calendar",
                    ControllerName="Academics",
                    ActionName="AcademicCalender",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=3, MenuText="Syllabus",
                    Link="https://syllabus.gtu.ac.in/Syllabus.aspx?tp=BE",
                    OpenInNewTab=true,
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=3, MenuText="Courses & Intake",
                    ControllerName="Institute", ActionName="AboutUs",
                    MenuType="Main", Position=3, IsVisible=true },

                new() { ParentId=3, MenuText="Research Grants",
                    ControllerName="Academics", ActionName="ResearchGrants",
                    MenuType="Main", Position=4, IsVisible=true },
 
                // ── R&D ───────────────────────────────────
                new() { ParentId=6, MenuText="SSIP",
                    ControllerName="CampusCommittee",
                    ActionName="CommitteePage",
                    DynamicId=2, DynamicType="Committee",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=6, MenuText="Research Grants",
                    ControllerName="Academics", ActionName="ResearchGrants",
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=6, MenuText="Patents",
                    Link="/DataFiles/Documents/IPR/IPR Details.pdf",
                    OpenInNewTab=true,
                    MenuType="Main", Position=3, IsVisible=true },
 
                // ── STUDENT CORNER ────────────────────────
                new() { ParentId=7, MenuText="General Rules",
                    ControllerName="StudentCorner", ActionName="Rules",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=7, MenuText="Time Table",
                    ControllerName="Documents", ActionName="Index",
                    DynamicId=60, DynamicType="Document",
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=7, MenuText="Student Clubs",
                    ControllerName="StudentCorner",
                    ActionName="StudentClubs",
                    MenuType="Main", Position=3, IsVisible=true },
 
                // ── ADMINISTRATION ────────────────────────
                new() { ParentId=99, MenuText="Principal",
                    ControllerName="Administration",
                    ActionName="Principal",
                    MenuType="Main", Position=1, IsVisible=true },

                new() { ParentId=99, MenuText="IQAC",
                    Link="/DataFiles/Documents/Rules/IQAC_GECP_2025_SIGNED.pdf",
                    OpenInNewTab=true,
                    MenuType="Main", Position=2, IsVisible=true },

                new() { ParentId=99, MenuText="Establishment Office",
                    ControllerName="Administration", ActionName="Esta",
                    MenuType="Main", Position=3, IsVisible=true },

                new() { ParentId=99, MenuText="Council of Heads",
                    ControllerName="Administration", ActionName="CoH",
                    MenuType="Main", Position=4, IsVisible=true },
 
                // ── FOOTER ────────────────────────────────
                new() { MenuText="Home",
                    ControllerName="Home", ActionName="Index",
                    MenuType="Footer", Position=1, IsVisible=true },

                new() { MenuText="About Us",
                    ControllerName="Institute", ActionName="AboutUs",
                    MenuType="Footer", Position=2, IsVisible=true },

                new() { MenuText="Placement",
                    ControllerName="PlacementCell", ActionName="Index",
                    MenuType="Footer", Position=3, IsVisible=true },

                new() { MenuText="Contact",
                    ControllerName="Institute", ActionName="Contact",
                    MenuType="Footer", Position=4, IsVisible=true },

                new() { MenuText="Downloads",
                    ControllerName="Documents", ActionName="TableView",
                    MenuType="Footer", Position=5, IsVisible=true },

                new() { MenuText="Alumni",
                    ControllerName="Alumni", ActionName="Index",
                    MenuType="Footer", Position=6, IsVisible=true },
            };

            // Use HasData-style insert with explicit IDs where set
            foreach (var item in items)
                context.MenuItems.Add(item);

            await context.SaveChangesAsync();
        }
    }
}