using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Admin.Migrations
{
    /// <inheritdoc />
    public partial class Marqee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFile",
                table: "Marquees");

            migrationBuilder.DropColumn(
                name: "MarqueeType",
                table: "Marquees");

            migrationBuilder.RenameColumn(
                name: "FileLink",
                table: "Marquees",
                newName: "FilePath");

            migrationBuilder.AlterColumn<string>(
                name: "ControllerName",
                table: "Marquees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionName",
                table: "Marquees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DynamicType",
                table: "Marquees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalLink",
                table: "Marquees",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkType",
                table: "Marquees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Marquees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "Marquees",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DynamicType",
                table: "Marquees");

            migrationBuilder.DropColumn(
                name: "ExternalLink",
                table: "Marquees");

            migrationBuilder.DropColumn(
                name: "LinkType",
                table: "Marquees");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Marquees");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "Marquees");

            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Marquees",
                newName: "FileLink");

            migrationBuilder.AlterColumn<string>(
                name: "ControllerName",
                table: "Marquees",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionName",
                table: "Marquees",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFile",
                table: "Marquees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MarqueeType",
                table: "Marquees",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
