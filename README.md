GEC Patan — College Website & Admin Panel
Overview
Complete college website management system for Government Engineering College, Patan.
Projects

GECPatan.Admin — Admin panel for managing all college content (RBAC protected)
GECPatan.Web — Public-facing college website (reads from DB)

Tech Stack

ASP.NET Core 8 MVC
Entity Framework Core 9
SQL Server (LocalDB for development)
ASP.NET Core Identity
AdminLTE 3 (Admin panel UI)
TinyMCE (Rich text editor)
SortableJS (Drag reorder)

Roles
RoleAccessSuperAdminEverythingPrincipalOwn profile + read allHODOwn department onlyFacultyOwn profile onlyContentEditorNews, sliders, documentsPlacementOfficerPlacement section only
Setup

Clone repo
Open GECPatan.sln in Visual Studio 2022
Update connection string in appsettings.json
Run migrations: dotnet ef database update
Run GECPatan.Admin — SuperAdmin account auto-created
Login: admin@gecpatan.ac.in / Admin@123456

Development Phases

Phase 1: Repo & Solution Setup ✅
Phase 2: Models & Database
Phase 3: Identity & Roles
Phase 4: Admin Layout & Dashboard
Phase 5: User Management
Phase 6: Department Management
Phase 7: Faculty Management
Phase 8: Dynamic Section Builder
Phase 9: Campus Committees
Phase 10: Activities & Achievements
Phase 11: Home Page Management
Phase 12: News & Institute Pages
Phase 13: Academics
Phase 14: Placement Cell
Phase 15: Student & Facilities
Phase 16: Administration & Alumni
Phase 17: Settings
Phase 18: RBAC Complete
Phase 19: Frontend (GECPatan.Web)
Phase 20: Testing & Go Live
ShareContent1774948124855_Controllers1774948124857_Models1774948124858_helpers1774948124859_Views1774948227021_Controllers1774948236800_Data1774948242328_Models1774948247884_Views1774948261218_ViewModel1774948818608_ZIP_BACK.zipzip1774948981739_ZIP_FRONT.zipzip
