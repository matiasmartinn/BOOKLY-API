using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModificacionInhabilitaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_schedule_unavailability");

            migrationBuilder.CreateTable(
                name: "service_unavailabilities",
                columns: table => new
                {
                    service_unavailability_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_unavailabilities", x => x.service_unavailability_id);
                    table.ForeignKey(
                        name: "FK_service_unavailabilities_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_unavailabilities_service_id",
                table: "service_unavailabilities",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_unavailabilities");

            migrationBuilder.CreateTable(
                name: "service_schedule_unavailability",
                columns: table => new
                {
                    service_schedule_unavailability_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service_id = table.Column<int>(type: "int", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedule_unavailability", x => x.service_schedule_unavailability_id);
                    table.ForeignKey(
                        name: "FK_service_schedule_unavailability_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_schedule_unavailability_service_id",
                table: "service_schedule_unavailability",
                column: "service_id");
        }
    }
}
