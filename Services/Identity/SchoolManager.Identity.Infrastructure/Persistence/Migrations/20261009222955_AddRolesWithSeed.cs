using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesWithSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "NameArabic", "NameEnglish", "Description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "مدير النظام", "SystemAdmin", "System administrator" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "مدير", "Admin", "Administrator" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "معلم", "Teacher", "Teacher" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "طالب", "Student", "Student" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "ولي أمر", "Parent", "Parent" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
