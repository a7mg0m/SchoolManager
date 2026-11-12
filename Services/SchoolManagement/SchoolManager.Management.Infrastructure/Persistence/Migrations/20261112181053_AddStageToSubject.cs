using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageToSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StageId",
                table: "Subjects",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE s SET s.StageId = g.StageId FROM Subjects s INNER JOIN Grades g ON g.Id = s.GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_StageId",
                table: "Subjects",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Stages_StageId",
                table: "Subjects",
                column: "StageId",
                principalTable: "Stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Stages_StageId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_StageId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "StageId",
                table: "Subjects");
        }
    }
}
