using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Admin.Migrations
{
    /// <inheritdoc />
    public partial class FinalMenuItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Link",
                table: "MenuItems",
                newName: "ExternalLink");

            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                table: "MenuItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "ExternalLink",
                table: "MenuItems",
                newName: "Link");
        }
    }
}
