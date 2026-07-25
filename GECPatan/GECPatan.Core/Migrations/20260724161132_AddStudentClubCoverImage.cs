using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentClubCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImagePath",
                table: "StudentClubs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImagePath",
                table: "StudentClubs");
        }
    }
}
