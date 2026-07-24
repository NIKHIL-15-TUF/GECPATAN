using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthYearTendorDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthYear",
                table: "TenderCategories");

            migrationBuilder.AddColumn<string>(
                name: "MonthYear",
                table: "TenderDocuments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthYear",
                table: "TenderDocuments");

            migrationBuilder.AddColumn<string>(
                name: "MonthYear",
                table: "TenderCategories",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
