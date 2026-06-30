using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddLetterNumberToFacultyApprovalInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovalLetterNumber",
                table: "FacultyApprovalInfos",
                newName: "LetterNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LetterNumber",
                table: "FacultyApprovalInfos",
                newName: "ApprovalLetterNumber");
        }
    }
}
