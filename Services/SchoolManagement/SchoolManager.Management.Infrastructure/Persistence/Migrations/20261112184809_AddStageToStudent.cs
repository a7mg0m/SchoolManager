using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Management.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStageToStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StageId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                "UPDATE s SET s.StageId = g.StageId FROM Students s INNER JOIN Grades g ON g.Id = s.GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StageId",
                table: "Students",
                column: "StageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Stages_StageId",
                table: "Students",
                column: "StageId",
                principalTable: "Stages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Stages_StageId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_StageId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StageId",
                table: "Students");
        }
    }
}
