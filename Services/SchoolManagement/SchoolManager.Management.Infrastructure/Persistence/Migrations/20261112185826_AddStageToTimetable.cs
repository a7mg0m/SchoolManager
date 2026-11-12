using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageToTimetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StageId",
                table: "Timetables",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE t SET t.StageId = g.StageId FROM Timetables t INNER JOIN Grades g ON g.Id = t.GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_StageId",
                table: "Timetables",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timetables_Stages_StageId",
                table: "Timetables",
                column: "StageId",
                principalTable: "Stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timetables_Stages_StageId",
                table: "Timetables");

            migrationBuilder.DropIndex(
                name: "IX_Timetables_StageId",
                table: "Timetables");

            migrationBuilder.DropColumn(
                name: "StageId",
                table: "Timetables");
        }
    }
}
