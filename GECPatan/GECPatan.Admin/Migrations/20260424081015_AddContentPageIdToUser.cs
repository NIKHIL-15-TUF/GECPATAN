using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPageIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContentPageId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentPageId",
                table: "AspNetUsers");
        }
    }
}
