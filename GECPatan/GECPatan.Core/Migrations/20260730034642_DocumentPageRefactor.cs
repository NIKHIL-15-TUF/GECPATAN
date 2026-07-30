using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GECPatan.Core.Migrations
{
    /// <inheritdoc />
    public partial class DocumentPageRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_CategoryId",
                table: "DocumentYearSections");

            migrationBuilder.DropTable(
                name: "DocumentCategories");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "DocumentYearSections",
                newName: "PageId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentYearSections_CategoryId",
                table: "DocumentYearSections",
                newName: "IX_DocumentYearSections_PageId");

            migrationBuilder.AlterColumn<int>(
                name: "YearSectionId",
                table: "DocumentFiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PageId",
                table: "DocumentFiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleBannerImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TableView = table.Column<bool>(type: "bit", nullable: false),
                    HasYearSections = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_PageId",
                table: "DocumentFiles",
                column: "PageId");

            // --- ADDED: SQL Cleanup to prevent Foreign Key conflicts ---
            // 1. Delete YearSections that point to non-existent Pages
            migrationBuilder.Sql("DELETE FROM [DocumentYearSections] WHERE [PageId] NOT IN (SELECT [Id] FROM [DocumentPages]);");

            // 2. Nullify YearSectionIds in Files if their parent YearSection was just deleted
            migrationBuilder.Sql("UPDATE [DocumentFiles] SET [YearSectionId] = NULL WHERE [YearSectionId] IS NOT NULL AND [YearSectionId] NOT IN (SELECT [Id] FROM [DocumentYearSections]);");

            // 3. Nullify PageIds in Files that point to non-existent Pages
            migrationBuilder.Sql("UPDATE [DocumentFiles] SET [PageId] = NULL WHERE [PageId] IS NOT NULL AND [PageId] NOT IN (SELECT [Id] FROM [DocumentPages]);");
            // -----------------------------------------------------------

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentPages_PageId",
                table: "DocumentFiles",
                column: "PageId",
                principalTable: "DocumentPages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles",
                column: "YearSectionId",
                principalTable: "DocumentYearSections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentYearSections_DocumentPages_PageId",
                table: "DocumentYearSections",
                column: "PageId",
                principalTable: "DocumentPages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentPages_PageId",
                table: "DocumentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentYearSections_DocumentPages_PageId",
                table: "DocumentYearSections");

            migrationBuilder.DropTable(
                name: "DocumentPages");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFiles_PageId",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "DocumentFiles");

            migrationBuilder.RenameColumn(
                name: "PageId",
                table: "DocumentYearSections",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentYearSections_PageId",
                table: "DocumentYearSections",
                newName: "IX_DocumentYearSections_CategoryId");

            migrationBuilder.AlterColumn<int>(
                name: "YearSectionId",
                table: "DocumentFiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDateInt = table.Column<long>(type: "bigint", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedDateInt = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentCategories", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_DocumentYearSections_YearSectionId",
                table: "DocumentFiles",
                column: "YearSectionId",
                principalTable: "DocumentYearSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentYearSections_DocumentCategories_CategoryId",
                table: "DocumentYearSections",
                column: "CategoryId",
                principalTable: "DocumentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}