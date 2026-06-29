using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlacementModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AveragePackage",
                table: "DisclosurePlacements");

            migrationBuilder.RenameColumn(
                name: "TotalStudents",
                table: "DisclosurePlacements",
                newName: "NoOfCompanies");

            migrationBuilder.RenameColumn(
                name: "TopRecruiter",
                table: "DisclosurePlacements",
                newName: "MinimumSalary");

            migrationBuilder.RenameColumn(
                name: "HighestPackage",
                table: "DisclosurePlacements",
                newName: "MaximumSalary");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NoOfCompanies",
                table: "DisclosurePlacements",
                newName: "TotalStudents");

            migrationBuilder.RenameColumn(
                name: "MinimumSalary",
                table: "DisclosurePlacements",
                newName: "TopRecruiter");

            migrationBuilder.RenameColumn(
                name: "MaximumSalary",
                table: "DisclosurePlacements",
                newName: "HighestPackage");

            migrationBuilder.AddColumn<string>(
                name: "AveragePackage",
                table: "DisclosurePlacements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
