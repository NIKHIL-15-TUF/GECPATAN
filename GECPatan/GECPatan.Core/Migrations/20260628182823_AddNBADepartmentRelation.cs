using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNBADepartmentRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentDeptId",
                table: "NBAAccreditations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NBAAccreditations_DepartmentDeptId",
                table: "NBAAccreditations",
                column: "DepartmentDeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_NBAAccreditations_Departments_DepartmentDeptId",
                table: "NBAAccreditations",
                column: "DepartmentDeptId",
                principalTable: "Departments",
                principalColumn: "DeptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NBAAccreditations_Departments_DepartmentDeptId",
                table: "NBAAccreditations");

            migrationBuilder.DropIndex(
                name: "IX_NBAAccreditations_DepartmentDeptId",
                table: "NBAAccreditations");

            migrationBuilder.DropColumn(
                name: "DepartmentDeptId",
                table: "NBAAccreditations");
        }
    }
}
