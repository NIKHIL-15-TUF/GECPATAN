using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDeptIdToFacultyTurnover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "FacultyTurnoverRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FacultyTurnoverRecords_DeptId",
                table: "FacultyTurnoverRecords",
                column: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_FacultyTurnoverRecords_Departments_DeptId",
                table: "FacultyTurnoverRecords",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacultyTurnoverRecords_Departments_DeptId",
                table: "FacultyTurnoverRecords");

            migrationBuilder.DropIndex(
                name: "IX_FacultyTurnoverRecords_DeptId",
                table: "FacultyTurnoverRecords");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "FacultyTurnoverRecords");
        }
    }
}
