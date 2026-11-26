using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Financial.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFeePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentFeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsBusFee = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFeePayments_StudentFees_StudentFeeId",
                        column: x => x.StudentFeeId,
                        principalTable: "StudentFees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_PaidOn",
                table: "StudentFeePayments",
                column: "PaidOn");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_StudentFeeId",
                table: "StudentFeePayments",
                column: "StudentFeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentFeePayments");
        }
    }
}
