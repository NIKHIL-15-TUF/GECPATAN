using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class ClubMember_FacultyLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ClubMembers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "ClubMembers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberType",
                table: "ClubMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ClubMembers_FacultyId",
                table: "ClubMembers",
                column: "FacultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMembers_Faculties_FacultyId",
                table: "ClubMembers",
                column: "FacultyId",
                principalTable: "Faculties",
                principalColumn: "FacultyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubMembers_Faculties_FacultyId",
                table: "ClubMembers");

            migrationBuilder.DropIndex(
                name: "IX_ClubMembers_FacultyId",
                table: "ClubMembers");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "ClubMembers");

            migrationBuilder.DropColumn(
                name: "MemberType",
                table: "ClubMembers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "ClubMembers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
