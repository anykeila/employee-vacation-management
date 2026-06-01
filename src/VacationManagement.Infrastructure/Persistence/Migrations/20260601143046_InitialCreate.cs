using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VacationManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacationRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Email", "ManagerId", "Name", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, "ana.silva@workflow.com", null, "Ana Silva", "", "Administrator" },
                    { 2, "joao.pereira@workflow.com", null, "João Pereira", "", "Manager" },
                    { 5, "joana.soares@workflow.com", null, "Joana Soares", "", "Manager" },
                    { 3, "marta.fernandes@workflow.com", 2, "Marta Fernandes", "", "Employee" },
                    { 4, "henrique.martins@workflow.com", 5, "Henrique Martins", "", "Employee" }
                });

            migrationBuilder.InsertData(
                table: "VacationRequests",
                columns: new[] { "Id", "CreatedAt", "EmployeeId", "EndDate", "Notes", "StartDate", "Status" },
                values: new object[,]
                {
                    { 3, new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 2, new DateOnly(2025, 9, 12), "Primeiras férias do ano", new DateOnly(2025, 9, 1), "Approved" },
                    { 1, new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 3, new DateOnly(2025, 8, 5), "Primeiras férias do ano", new DateOnly(2025, 8, 1), "Approved" },
                    { 2, new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, new DateOnly(2025, 8, 15), "Viagem em família", new DateOnly(2025, 8, 10), "Approved" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagerId",
                table: "Employees",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_EmployeeId",
                table: "VacationRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_Status_StartDate_EndDate",
                table: "VacationRequests",
                columns: new[] { "Status", "StartDate", "EndDate" });

            // Global rule: no two employees may hold APPROVED vacations on the same day.
            // Enforced atomically at the database level (concurrency-safe) via a GiST
            // exclusion constraint over the inclusive date range. See README.
            migrationBuilder.Sql(
                """
                ALTER TABLE "VacationRequests"
                ADD CONSTRAINT "EX_VacationRequests_NoOverlap_WhenApproved"
                EXCLUDE USING gist ((daterange("StartDate", "EndDate", '[]')) WITH &&)
                WHERE ("Status" = 'Approved');
                """);

            // Seeded rows use fixed ids (1..5 / 1..3). Restart identity well above them
            // so newly created rows never collide with the reference data.
            migrationBuilder.Sql(@"ALTER TABLE ""Employees"" ALTER COLUMN ""Id"" RESTART WITH 1000;");
            migrationBuilder.Sql(@"ALTER TABLE ""VacationRequests"" ALTER COLUMN ""Id"" RESTART WITH 1000;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""VacationRequests"" DROP CONSTRAINT IF EXISTS ""EX_VacationRequests_NoOverlap_WhenApproved"";");

            migrationBuilder.DropTable(
                name: "VacationRequests");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
