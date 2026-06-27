using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class FacultyEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrainingType",
                table: "FacultyTrainings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrainingType",
                table: "FacultyTrainings");
        }
    }
}
