using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Core.Data
{
    public static class MenuItemSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            if (await db.MenuItems.AnyAsync()) return;

            // Helper
            MenuItem M(string text, int? parentId, string linkType,
                string? ctrl = null, string? action = null,
                int? dynId = null, string? dynType = null,
                string? link = null, int pos = 0,
                bool visible = true, bool newTab = false,
                string menuType = "Main", string? css = null)
                => new()
                {
                    MenuText = text,
                    ParentId = parentId,
                    LinkType = linkType,
                    ControllerName = ctrl,
                    ActionName = action,
                    DynamicId = dynId,
                    DynamicType = dynType,
                    ExternalLink = link,
                    Position = pos,
                    IsVisible = visible,
                    OpenInNewTab = newTab,
                    MenuType = menuType,
                    CssClass = css
                };

            // ═══════════════════════════════════════════════
            // LEVEL 1 — TOP NAVBAR
            // ═══════════════════════════════════════════════
            var home = M("Home", null, "internal", "Home", "Index", pos: 0);
            var inst = M("Institute", null, "none", pos: 1);
            var acad = M("Academics", null, "none", pos: 2);
            var depts = M("Departments", null, "none", pos: 3);
            var rnd = M("R&D", null, "none", pos: 4);
            var stcorn = M("Student Corner", null, "none", pos: 5);
            var place = M("Placement", null, "internal", "PlacementCell", "Index", pos: 6);
            var admin = M("Administration", null, "none", pos: 7);
            var dl = M("Downloads", null, "dynamic", "Documents", "TableView",
                           dynId: 100, dynType: "Document", pos: 8);
            var nba = M("NBA Data", null, "dynamic", "Documents", "TableView",
                           pos: 9, css: "MenuOrange");

            db.MenuItems.AddRange(
                home, inst, acad, depts, rnd, stcorn, place, admin, dl, nba);
            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // INSTITUTE CHILDREN
            // ═══════════════════════════════════════════════
            db.MenuItems.AddRange(
                M("About Us", inst.Id, "internal", "Institute", "AboutUs", pos: 0),
                M("Governance", inst.Id, "external", link: "/ImpDocs/GEC_Organizational_Chart.pdf",
                  newTab: true, pos: 1),
                M("Mandatory Disclosure", inst.Id, "dynamic", "Documents", "Index",
                  dynId: 30, dynType: "Document", pos: 2),
                M("MoUs", inst.Id, "dynamic", "Documents", "TableView",
                  dynId: 50, dynType: "Document", pos: 3),
                M("Newsletter", inst.Id, "internal", "Institute", "NewsLetter", pos: 4),
                M("RTI", inst.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 6, dynType: "Committee", pos: 5),
                M("Information Booklet", inst.Id, "external",
                  link: "/ImpDocs/Brochure_GECP.pdf", newTab: true, pos: 6)
            );

            // Facilities (sub-menu under Institute)
            var facSub = M("Facilities", inst.Id, "none", pos: 7);
            db.MenuItems.Add(facSub);
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                M("Library", facSub.Id, "internal", "Facilities", "Library", pos: 0),
                M("Hostel", facSub.Id, "internal", "Facilities", "Hostel", pos: 1),
                M("Medical Facility", facSub.Id, "internal", "Facilities", "MedicalFacility", pos: 2),
                M("Transportation", facSub.Id, "internal", "Facilities", "MedicalFacility", pos: 3),
                M("Central Facilities", facSub.Id, "internal", "Facilities", "CentralFacilities", pos: 4),
                M("Center of Excellence", facSub.Id, "internal", "Facilities", "CenterOfExcellence", pos: 5)
            );

            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // ACADEMICS CHILDREN
            // ═══════════════════════════════════════════════
            db.MenuItems.AddRange(
                M("Academic Calendar", acad.Id, "internal", "Academics", "AcademicCalender", pos: 0),
                M("Admission Process", acad.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 16, dynType: "Committee", pos: 1),
                M("Courses & Intake", acad.Id, "internal", "Institute", "AboutUs", pos: 2),
                M("Syllabus", acad.Id, "external",
                  link: "https://syllabus.gtu.ac.in/Syllabus.aspx?tp=BE",
                  newTab: true, pos: 3)
            );
            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // DEPARTMENTS CHILDREN
            // ═══════════════════════════════════════════════
            db.MenuItems.AddRange(
                M("Electronics & Communication", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 1, dynType: "Department", pos: 0),
                M("Computer Science & Engineering", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 2, dynType: "Department", pos: 1),
                M("Electrical Engineering", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 3, dynType: "Department", pos: 2),
                M("Mechanical Engineering", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 5, dynType: "Department", pos: 3),
                M("Science & Humanities", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 6, dynType: "Department", pos: 4),
                M("Applied Mechanics", depts.Id, "dynamic",
                  "Department", "Departments", dynId: 8, dynType: "Department", pos: 5)
            );

            // Civil & Applied Mechanics container
            var civilParent = M("Civil Engineering & Applied Mechanics",
                depts.Id, "none", pos: 6);
            db.MenuItems.Add(civilParent);
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                M("Civil Engineering", civilParent.Id, "dynamic",
                  "Department", "DepartmentDetails", dynId: 4, dynType: "Department", pos: 0),
                M("Applied Mechanics", civilParent.Id, "dynamic",
                  "Department", "DepartmentDetails", dynId: 8, dynType: "Department", pos: 1)
            );
            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // R&D CHILDREN
            // ═══════════════════════════════════════════════
            db.MenuItems.AddRange(
                M("Center of Excellence", rnd.Id, "internal", "Facilities", "CenterOfExcellence", pos: 0),
                M("SSIP", rnd.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 2, dynType: "Committee", pos: 1),
                M("Design & Tinkering Lab", rnd.Id, "internal", "Research", "DesignLab", pos: 2),
                M("Research Grants", rnd.Id, "internal", "Academics", "ResearchGrants", pos: 3),
                M("IIRS ISRO Nodal Centre", rnd.Id, "internal", "Facilities", "isro", pos: 4),
                M("Patents", rnd.Id, "external",
                  link: "/DataFiles/Documents/IPR/IPR Details.pdf", newTab: true, pos: 5)
            );
            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // STUDENT CORNER CHILDREN
            // ═══════════════════════════════════════════════

            // Student Support System sub-menu
            var sss = M("Student Support System", stcorn.Id, "none", pos: 0);
            db.MenuItems.Add(sss);
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                M("Psychology Cell", sss.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 12, dynType: "Committee", pos: 0),
                M("SC/ST Committee", sss.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 7, dynType: "Committee", pos: 1),
                M("ICC", sss.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 13, dynType: "Committee", pos: 2),
                M("OMBUDSMAN", sss.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 14, dynType: "Committee", pos: 3),
                M("Student Counsellor", sss.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 5, dynType: "Committee", pos: 4),
                M("National Task Force", sss.Id, "external",
                  link: "/DataFiles/CampusCommittees/Psychology/NationalTaskForce.pdf",
                  newTab: true, pos: 5),
                M("Anti-Ragging Squad", sss.Id, "external",
                  link: "/DataFiles/Documents/Student Suppot System/Anti Ragging Scaud.pdf",
                  newTab: true, pos: 6),
                M("Women Helpline", sss.Id, "external",
                  link: "/DataFiles/Documents/Student Suppot System/Woman Helpline.pdf",
                  newTab: true, pos: 7),
                M("Industry Institute Cell", sss.Id, "external",
                  link: "/DataFiles/Documents/Student Suppot System/Industry Institute Interaction Cell.pdf",
                  newTab: true, pos: 8),
                M("Post Grievance", sss.Id, "external",
                  link: "/DataFiles/Documents/Grievance/Post_Grievance.pdf",
                  newTab: true, pos: 9)
            );

            db.MenuItems.AddRange(
                M("General Rules", stcorn.Id, "internal", "StudentCorner", "Rules", pos: 1),
                M("Academic Rules", stcorn.Id, "external",
                  link: "/DataFiles/Documents/Rules/General Rules and Regulations for Students.pdf",
                  newTab: true, pos: 2),
                M("NTPEL Local Chapter", stcorn.Id, "dynamic", "CampusCommittee", "CommitteePage",
                  dynId: 15, dynType: "Committee", pos: 3),
                M("Time Table", stcorn.Id, "dynamic", "Documents", "Index",
                  dynId: 60, dynType: "Document", pos: 4),
                M("Enrollment Details", stcorn.Id, "dynamic", "Documents", "Index",
                  dynId: 5, dynType: "Document", pos: 5),
                M("Student Clubs", stcorn.Id, "internal", "StudentCorner", "StudentClubs", pos: 6),
                M("Student Grade History", stcorn.Id, "external",
                  link: "https://www.students.gtu.ac.in/Default.aspx", newTab: true, pos: 7),
                M("Fees Portal", stcorn.Id, "external",
                  link: "https://www.onlinesbi.sbi/sbicollect/icollecthome.htm",
                  newTab: true, pos: 8)
            );
            await db.SaveChangesAsync();

            // ═══════════════════════════════════════════════
            // ADMINISTRATION CHILDREN
            // ═══════════════════════════════════════════════
            var adminRules = M("Administrative Rules", admin.Id, "none", pos: 2);
            var commCell = M("Committees / Cell", admin.Id, "none", pos: 3);
            db.MenuItems.AddRange(adminRules, commCell);

            db.MenuItems.AddRange(
                M("Principal", admin.Id, "internal", "Administration", "Principal", pos: 0),
                M("IQAC", admin.Id, "external",
                  link: "/DataFiles/Documents/Rules/IQAC_GECP_2025_SIGNED.pdf",
                  newTab: true, pos: 1),
                M("Establishment Office", admin.Id, "internal", "Administration", "Esta", pos: 4),
                M("Council of Heads", admin.Id, "internal", "Administration", "CoH", pos: 5),
                M("Faculty Data Mgmt", admin.Id, "external",
                  link: "/DataFiles/Documents/Rules/Standardized Data Management and Faculty Transparency.pdf",
                  newTab: true, pos: 6),
                M("Central Store", admin.Id, "internal", "Administration", "Central_Store", pos: 7)
            );
            await db.SaveChangesAsync();

            // Administrative Rules children
            db.MenuItems.AddRange(
                M("Institute Admin Rules", adminRules.Id, "external",
                  link: "/DataFiles/Documents/Rules/1 Institute Administrative Rules.pdf",
                  newTab: true, pos: 0),
                M("Leave Rules", adminRules.Id, "external",
                  link: "/DataFiles/Documents/Rules/LEAVE.pdf", newTab: true, pos: 1),
                M("Pension Rules", adminRules.Id, "external",
                  link: "/DataFiles/Documents/Rules/PENSION.pdf", newTab: true, pos: 2),
                M("NPS", adminRules.Id, "external",
                  link: "/DataFiles/Documents/Rules/NPS.pdf", newTab: true, pos: 3)
            );

            // Committees / Cell children
            db.MenuItems.AddRange(
                 M("Women Development Cell", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 1, dynType: "Committee", pos: 0),
                 M("SSIP", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 2, dynType: "Committee", pos: 1),
                 M("NSS", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 3, dynType: "Committee", pos: 2),
                 M("Anti Ragging Cell", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 4, dynType: "Committee", pos: 3),
                 M("Student Counsellor", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 5, dynType: "Committee", pos: 4),
                 M("SC/ST Committee", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 7, dynType: "Committee", pos: 5),
                 M("Student Section", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 8, dynType: "Committee", pos: 6),
                 M("Gymkhana", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 10, dynType: "Committee", pos: 7),
                 M("Psychology Cell", commCell.Id, "dynamic", "Committee", "Index",
                   dynId: 12, dynType: "Committee", pos: 8)
             );
            await db.SaveChangesAsync();
            // ═══════════════════════════════════════════════
            // DOWNLOADS CHILDREN
            // ═══════════════════════════════════════════════
            db.MenuItems.Add(
                M("Tenders", dl.Id, "internal", "Institute", "TendersTableView", pos: 0)
            );

            // ═══════════════════════════════════════════════
            // NBA DATA CHILDREN
            // ═══════════════════════════════════════════════
            var placementData = M("Placement Data", nba.Id, "none", pos: 2);
            db.MenuItems.AddRange(
                M("Faculty Information", nba.Id, "external",
                  link: "/DataFiles/NBA/Faculty Information.pdf", newTab: true, pos: 0),
                M("Mechanical Student Details", nba.Id, "external",
                  link: "/DataFiles/NBA/Mechanical Student Details.pdf", newTab: true, pos: 1),
                placementData
            );
            await db.SaveChangesAsync();

            db.MenuItems.AddRange(
                M("2022-2023", placementData.Id, "external",
                  link: "/DataFiles/NBA/Placement_2022-2023.pdf", newTab: true, pos: 0),
                M("2023-2024", placementData.Id, "external",
                  link: "/DataFiles/NBA/Placement_2023-2024.pdf", newTab: true, pos: 1),
                M("2024-2025", placementData.Id, "external",
                  link: "/DataFiles/NBA/Placement_2024-2025.pdf", newTab: true, pos: 2)
            );

            // ═══════════════════════════════════════════════
            // FOOTER MENU
            // ═══════════════════════════════════════════════
            db.MenuItems.AddRange(
                M("Home", null, "internal", "Home", "Index", pos: 0, menuType: "Footer"),
                M("About Us", null, "internal", "Institute", "AboutUs", pos: 1, menuType: "Footer"),
                M("Departments", null, "none", pos: 2, menuType: "Footer"),
                M("Placement", null, "internal", "PlacementCell", "Index", pos: 3, menuType: "Footer"),
                M("Alumni", null, "internal", "Alumni", "Index", pos: 4, menuType: "Footer"),
                M("Downloads", null, "dynamic", "Documents", "TableView",
                  dynId: 100, dynType: "Document", pos: 5, menuType: "Footer"),
                M("Contact", null, "internal", "Institute", "Contact", pos: 6, menuType: "Footer")
            );

            await db.SaveChangesAsync();
        }
    }
}