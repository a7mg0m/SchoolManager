using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileLinkedIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            // Existing data: employees are linked through Employees.UserId;
            // teacher logins (created by the teacher sync) use the teacher id as the user id.
            // Parents have no login accounts yet, so ParentId stays empty.
            migrationBuilder.Sql(@"
UPDATE p SET p.EmployeeId = e.Id
FROM UserProfiles p
INNER JOIN Employees e ON e.UserId = p.UserId;

UPDATE p SET p.TeacherId = u.Id
FROM UserProfiles p
INNER JOIN Users u ON u.Id = p.UserId
WHERE u.RoleId = '00000000-0000-0000-0000-000000000003'
  AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.UserId = u.Id);
");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_EmployeeId",
                table: "UserProfiles",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ParentId",
                table: "UserProfiles",
                column: "ParentId",
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TeacherId",
                table: "UserProfiles",
                column: "TeacherId",
                unique: true,
                filter: "[TeacherId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_EmployeeId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ParentId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TeacherId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "UserProfiles");
        }
    }
}
