using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Attendance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAttendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimetableSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Focus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Interaction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Behavior = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HealthStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvaluationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Buses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlateNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DriverPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BusRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buses_BusRoutes_BusRouteId",
                        column: x => x.BusRouteId,
                        principalTable: "BusRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusRouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusRouteStops_BusRoutes_BusRouteId",
                        column: x => x.BusRouteId,
                        principalTable: "BusRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PeriodAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyAttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimetableSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AbsenceReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodAttendances_DailyAttendances_DailyAttendanceId",
                        column: x => x.DailyAttendanceId,
                        principalTable: "DailyAttendances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CurrentLatitude = table.Column<double>(type: "float", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "float", nullable: true),
                    LastLocationAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusRouteStopId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FeeCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StudentFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusAssignments_BusRouteStops_BusRouteStopId",
                        column: x => x.BusRouteStopId,
                        principalTable: "BusRouteStops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusAssignments_Buses_BusId",
                        column: x => x.BusId,
                        principalTable: "Buses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripStudents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TripId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BoardedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DroppedOffAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripStudents_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_AcademicYearId",
                table: "BusAssignments",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_BusId_IsActive",
                table: "BusAssignments",
                columns: new[] { "BusId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_BusRouteStopId",
                table: "BusAssignments",
                column: "BusRouteStopId");

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_StudentFeeId",
                table: "BusAssignments",
                column: "StudentFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BusAssignments_StudentId_AcademicYearId",
                table: "BusAssignments",
                columns: new[] { "StudentId", "AcademicYearId" });

            migrationBuilder.CreateIndex(
                name: "IX_Buses_BusRouteId",
                table: "Buses",
                column: "BusRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Buses_PlateNumber",
                table: "Buses",
                column: "PlateNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusRouteStops_BusRouteId_Order",
                table: "BusRouteStops",
                columns: new[] { "BusRouteId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_AcademicYearId",
                table: "DailyAttendances",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_GradeId",
                table: "DailyAttendances",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_SectionId_Date",
                table: "DailyAttendances",
                columns: new[] { "SectionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_StudentId_Date",
                table: "DailyAttendances",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRequests_ParentId",
                table: "EvaluationRequests",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRequests_StudentId",
                table: "EvaluationRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRequests_SubjectId",
                table: "EvaluationRequests",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRequests_TeacherId_Status",
                table: "EvaluationRequests",
                columns: new[] { "TeacherId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn_OccurredOn",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodAttendances_DailyAttendanceId_TimetableSlotId",
                table: "PeriodAttendances",
                columns: new[] { "DailyAttendanceId", "TimetableSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodAttendances_SubjectId",
                table: "PeriodAttendances",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodAttendances_TeacherId",
                table: "PeriodAttendances",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodAttendances_TimetableSlotId",
                table: "PeriodAttendances",
                column: "TimetableSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_EvaluationRequestId",
                table: "StudentEvaluations",
                column: "EvaluationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_StudentId_TimetableSlotId_Date",
                table: "StudentEvaluations",
                columns: new[] { "StudentId", "TimetableSlotId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_SubjectId",
                table: "StudentEvaluations",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_TeacherId",
                table: "StudentEvaluations",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluations_TimetableSlotId",
                table: "StudentEvaluations",
                column: "TimetableSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_BusId_Date_Type",
                table: "Trips",
                columns: new[] { "BusId", "Date", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Date_Status",
                table: "Trips",
                columns: new[] { "Date", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TripStudents_StudentId",
                table: "TripStudents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TripStudents_TripId_StudentId",
                table: "TripStudents",
                columns: new[] { "TripId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusAssignments");

            migrationBuilder.DropTable(
                name: "EvaluationRequests");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PeriodAttendances");

            migrationBuilder.DropTable(
                name: "StudentEvaluations");

            migrationBuilder.DropTable(
                name: "TripStudents");

            migrationBuilder.DropTable(
                name: "BusRouteStops");

            migrationBuilder.DropTable(
                name: "DailyAttendances");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Buses");

            migrationBuilder.DropTable(
                name: "BusRoutes");
        }
    }
}
