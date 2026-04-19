using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Admin.Migrations
{
    /// <inheritdoc />
    public partial class ChangesModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentImages_Departments_DepartmentDeptId",
                table: "DepartmentImages");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentMissions_Departments_DepartmentDeptId",
                table: "DepartmentMissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentPEOs_Departments_DepartmentDeptId",
                table: "DepartmentPEOs");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentPSOs_Departments_DepartmentDeptId",
                table: "DepartmentPSOs");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentVisions_Departments_DepartmentDeptId",
                table: "DepartmentVisions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_DocumentYearSectionId",
                table: "DocumentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_DocumentCategoryId",
                table: "DocumentYearSections");

            migrationBuilder.DropTable(
                name: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentVisions_DepartmentDeptId",
                table: "DepartmentVisions");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentPSOs_DepartmentDeptId",
                table: "DepartmentPSOs");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentPEOs_DepartmentDeptId",
                table: "DepartmentPEOs");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentMissions_DepartmentDeptId",
                table: "DepartmentMissions");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentImages_DepartmentDeptId",
                table: "DepartmentImages");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "StudentClubs");

            migrationBuilder.DropColumn(
                name: "CourseCode",
                table: "ProgramIntakes");

            migrationBuilder.DropColumn(
                name: "ProgramName",
                table: "ProgramIntakes");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PlacementTeamMembers");

            migrationBuilder.DropColumn(
                name: "Mobile",
                table: "PlacementTeamMembers");

            migrationBuilder.DropColumn(
                name: "AverageCTC",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "Business",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "HigherStudy",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "PlacementPercentage",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "FacultyTrainings");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "FacultyExperiences");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "DocumentYearSections");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "MonthYear",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "DepartmentVisions");

            migrationBuilder.DropColumn(
                name: "FacultyCount",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HODImagePath",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HODMessage",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HODName",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "Intake",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "LabCount",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "DepartmentPSOs");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "DepartmentPEOs");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "DepartmentMissions");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "DepartmentImages");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "DepartmentImages");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "ProgramIntakes",
                newName: "IntakeYear");

            migrationBuilder.RenameColumn(
                name: "Placed",
                table: "PlacementStatistics",
                newName: "TotalStudents");

            migrationBuilder.RenameColumn(
                name: "Passout",
                table: "PlacementStatistics",
                newName: "TotalPlaced");

            migrationBuilder.RenameColumn(
                name: "IsDisplay",
                table: "PlacementStatistics",
                newName: "IsVisible");

            migrationBuilder.RenameColumn(
                name: "DocumentCategoryId",
                table: "DocumentYearSections",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentYearSections_DocumentCategoryId",
                table: "DocumentYearSections",
                newName: "IX_DocumentYearSections_CategoryId");

            migrationBuilder.RenameColumn(
                name: "DocumentYearSectionId",
                table: "DocumentFiles",
                newName: "YearSectionId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentFiles_DocumentYearSectionId",
                table: "DocumentFiles",
                newName: "IX_DocumentFiles_YearSectionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "ResearchGrants",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletionDate",
                table: "ResearchGrants",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "ProgramIntakes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "ProgramIntakes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Year",
                table: "PlacementStatistics",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "AveragePackage",
                table: "PlacementStatistics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HighestPackage",
                table: "PlacementStatistics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "PersonalDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "PersonalDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "PersonalDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MonthYear",
                table: "MoUDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadDate",
                table: "ImportantDocuments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "ImportantDocuments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "FromDate",
                table: "FacultyTrainings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ToDate",
                table: "FacultyTrainings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FromDate",
                table: "FacultyExperiences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ToDate",
                table: "FacultyExperiences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tagline",
                table: "Departments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "ClubImages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordId = table.Column<int>(type: "int", nullable: true),
                    RecordName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Labs",
                columns: table => new
                {
                    LabId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    LabName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    About = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labs", x => x.LabId);
                    table.ForeignKey(
                        name: "FK_Labs_Departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "Departments",
                        principalColumn: "DeptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timetables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeptId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    SemesterType = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timetables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timetables_Departments_DeptId",
                        column: x => x.DeptId,
                        principalTable: "Departments",
                        principalColumn: "DeptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabImages",
                columns: table => new
                {
                    LabImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabImages", x => x.LabImageId);
                    table.ForeignKey(
                        name: "FK_LabImages_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "LabId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderCategoryId = table.Column<int>(type: "int", nullable: false),
                    DocTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderDocuments_TenderCategories_TenderCategoryId",
                        column: x => x.TenderCategoryId,
                        principalTable: "TenderCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramIntakes_DeptId_IntakeYear",
                table: "ProgramIntakes",
                columns: new[] { "DeptId", "IntakeYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentVisions_DeptId",
                table: "DepartmentVisions",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPSOs_DeptId",
                table: "DepartmentPSOs",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPEOs_DeptId",
                table: "DepartmentPEOs",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMissions_DeptId",
                table: "DepartmentMissions",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentImages_DeptId",
                table: "DepartmentImages",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPages_Slug",
                table: "ContentPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabImages_LabId",
                table: "LabImages",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_DeptId",
                table: "Labs",
                column: "DeptId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderCategoryId",
                table: "TenderDocuments",
                column: "TenderCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_DeptId",
                table: "Timetables",
                column: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentImages_Departments_DeptId",
                table: "DepartmentImages",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMissions_Departments_DeptId",
                table: "DepartmentMissions",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentPEOs_Departments_DeptId",
                table: "DepartmentPEOs",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentPSOs_Departments_DeptId",
                table: "DepartmentPSOs",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentVisions_Departments_DeptId",
                table: "DepartmentVisions",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles",
                column: "YearSectionId",
                principalTable: "DocumentYearSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_CategoryId",
                table: "DocumentYearSections",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramIntakes_Departments_DeptId",
                table: "ProgramIntakes",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentImages_Departments_DeptId",
                table: "DepartmentImages");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentMissions_Departments_DeptId",
                table: "DepartmentMissions");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentPEOs_Departments_DeptId",
                table: "DepartmentPEOs");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentPSOs_Departments_DeptId",
                table: "DepartmentPSOs");

            migrationBuilder.DropForeignKey(
                name: "FK_DepartmentVisions_Departments_DeptId",
                table: "DepartmentVisions");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_CategoryId",
                table: "DocumentYearSections");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramIntakes_Departments_DeptId",
                table: "ProgramIntakes");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ContentPages");

            migrationBuilder.DropTable(
                name: "LabImages");

            migrationBuilder.DropTable(
                name: "TenderDocuments");

            migrationBuilder.DropTable(
                name: "Timetables");

            migrationBuilder.DropTable(
                name: "Labs");

            migrationBuilder.DropTable(
                name: "TenderCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProgramIntakes_DeptId_IntakeYear",
                table: "ProgramIntakes");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentVisions_DeptId",
                table: "DepartmentVisions");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentPSOs_DeptId",
                table: "DepartmentPSOs");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentPEOs_DeptId",
                table: "DepartmentPEOs");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentMissions_DeptId",
                table: "DepartmentMissions");

            migrationBuilder.DropIndex(
                name: "IX_DepartmentImages_DeptId",
                table: "DepartmentImages");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "ProgramIntakes");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "ProgramIntakes");

            migrationBuilder.DropColumn(
                name: "AveragePackage",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "HighestPackage",
                table: "PlacementStatistics");

            migrationBuilder.DropColumn(
                name: "FromDate",
                table: "FacultyTrainings");

            migrationBuilder.DropColumn(
                name: "ToDate",
                table: "FacultyTrainings");

            migrationBuilder.DropColumn(
                name: "FromDate",
                table: "FacultyExperiences");

            migrationBuilder.DropColumn(
                name: "ToDate",
                table: "FacultyExperiences");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "ClubImages");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "IntakeYear",
                table: "ProgramIntakes",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "TotalStudents",
                table: "PlacementStatistics",
                newName: "Placed");

            migrationBuilder.RenameColumn(
                name: "TotalPlaced",
                table: "PlacementStatistics",
                newName: "Passout");

            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "PlacementStatistics",
                newName: "IsDisplay");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "DocumentYearSections",
                newName: "DocumentCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentYearSections_CategoryId",
                table: "DocumentYearSections",
                newName: "IX_DocumentYearSections_DocumentCategoryId");

            migrationBuilder.RenameColumn(
                name: "YearSectionId",
                table: "DocumentFiles",
                newName: "DocumentYearSectionId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentFiles_YearSectionId",
                table: "DocumentFiles",
                newName: "IX_DocumentFiles_DocumentYearSectionId");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "StudentClubs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StartDate",
                table: "ResearchGrants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompletionDate",
                table: "ResearchGrants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CourseCode",
                table: "ProgramIntakes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramName",
                table: "ProgramIntakes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PlacementTeamMembers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                table: "PlacementTeamMembers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Year",
                table: "PlacementStatistics",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageCTC",
                table: "PlacementStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PlacementStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "PlacementStatistics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Business",
                table: "PlacementStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HigherStudy",
                table: "PlacementStatistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PlacementPercentage",
                table: "PlacementStatistics",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "PersonalDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "PersonalDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Contact",
                table: "PersonalDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MonthYear",
                table: "MoUDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UploadDate",
                table: "ImportantDocuments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileType",
                table: "ImportantDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "FacultyTrainings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "FacultyExperiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "Faculties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisible",
                table: "DocumentYearSections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "DocumentFiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MonthYear",
                table: "DocumentFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadDate",
                table: "DocumentFiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "DepartmentVisions",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tagline",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FacultyCount",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HODImagePath",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODMessage",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HODName",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Intake",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LabCount",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "DepartmentPSOs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "DepartmentPEOs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "DepartmentMissions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "DepartmentImages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "DepartmentImages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    UploadDate = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentVisions_DepartmentDeptId",
                table: "DepartmentVisions",
                column: "DepartmentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPSOs_DepartmentDeptId",
                table: "DepartmentPSOs",
                column: "DepartmentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentPEOs_DepartmentDeptId",
                table: "DepartmentPEOs",
                column: "DepartmentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentMissions_DepartmentDeptId",
                table: "DepartmentMissions",
                column: "DepartmentDeptId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentImages_DepartmentDeptId",
                table: "DepartmentImages",
                column: "DepartmentDeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentImages_Departments_DepartmentDeptId",
                table: "DepartmentImages",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentMissions_Departments_DepartmentDeptId",
                table: "DepartmentMissions",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentPEOs_Departments_DepartmentDeptId",
                table: "DepartmentPEOs",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentPSOs_Departments_DepartmentDeptId",
                table: "DepartmentPSOs",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DepartmentVisions_Departments_DepartmentDeptId",
                table: "DepartmentVisions",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_DocumentYearSectionId",
                table: "DocumentFiles",
                column: "DocumentYearSectionId",
                principalTable: "DocumentYearSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_DocumentCategoryId",
                table: "DocumentYearSections",
                column: "DocumentCategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
