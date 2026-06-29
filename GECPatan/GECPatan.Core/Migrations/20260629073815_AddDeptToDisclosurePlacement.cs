using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDeptToDisclosurePlacement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeptId",
                table: "DisclosurePlacements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DisclosurePlacements_DeptId",
                table: "DisclosurePlacements",
                column: "DeptId");

            migrationBuilder.AddForeignKey(
                name: "FK_DisclosurePlacements_Departments_DeptId",
                table: "DisclosurePlacements",
                column: "DeptId",
                principalTable: "Departments",
                principalColumn: "DeptId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DisclosurePlacements_Departments_DeptId",
                table: "DisclosurePlacements");

            migrationBuilder.DropIndex(
                name: "IX_DisclosurePlacements_DeptId",
                table: "DisclosurePlacements");

            migrationBuilder.DropColumn(
                name: "DeptId",
                table: "DisclosurePlacements");
        }
    }
}
