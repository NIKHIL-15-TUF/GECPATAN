using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Admin.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacilityVision_Facilities_FacilityId",
                table: "FacilityVision");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FacilityVision",
                table: "FacilityVision");

            migrationBuilder.DropColumn(
                name: "LastLoginDate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImagePath",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "FacilityVision",
                newName: "FacilityVisions");

            migrationBuilder.RenameIndex(
                name: "IX_FacilityVision_FacilityId",
                table: "FacilityVisions",
                newName: "IX_FacilityVisions_FacilityId");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "CommitteeId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FacilityVisions",
                table: "FacilityVisions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityVisions_Facilities_FacilityId",
                table: "FacilityVisions",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacilityVisions_Facilities_FacilityId",
                table: "FacilityVisions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FacilityVisions",
                table: "FacilityVisions");

            migrationBuilder.DropColumn(
                name: "CommitteeId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "FacilityVisions",
                newName: "FacilityVision");

            migrationBuilder.RenameIndex(
                name: "IX_FacilityVisions_FacilityId",
                table: "FacilityVision",
                newName: "IX_FacilityVision_FacilityId");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImagePath",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FacilityVision",
                table: "FacilityVision",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FacilityVision_Facilities_FacilityId",
                table: "FacilityVision",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
