using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_id = table.Column<int>(type: "int", nullable: false),
                    assigned_secretary_id = table.Column<int>(type: "int", nullable: true),
                    client_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    client_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    client_email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    start_date_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    end_date_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    client_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    internal_notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    update_on = table.Column<DateTime>(type: "datetime2", nullable: true),
                    update_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    service_type_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_types", x => x.service_type_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    last_login_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "appointment_field_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    field_definition_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_field_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_field_values_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    service_type_id = table.Column<int>(type: "int", nullable: false),
                    duration_minutes = table.Column<int>(type: "int", nullable: false),
                    mode = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.service_id);
                    table.ForeignKey(
                        name: "FK_services_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "service_type_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_services_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_schedule_unavailability",
                columns: table => new
                {
                    service_schedule_unavailability_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    service_id = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "service_schedules",
                columns: table => new
                {
                    service_schedule_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    day = table.Column<int>(type: "int", nullable: false),
                    service_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_schedules", x => x.service_schedule_id);
                    table.ForeignKey(
                        name: "FK_service_schedules_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_secretaries",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "int", nullable: false),
                    secretary_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_secretaries", x => new { x.service_id, x.secretary_id });
                    table.ForeignKey(
                        name: "FK_service_secretaries_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_secretaries_users_secretary_id",
                        column: x => x.secretary_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "service_types",
                columns: new[] { "service_type_id", "description", "is_active", "name" },
                values: new object[,]
                {
                    { 1, "Consulta médica", true, "Consulta" },
                    { 2, "Sesión de tratamiento", true, "Tratamiento" },
                    { 3, "Consulta de seguimiento", true, "Seguimiento" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_field_values_appointment_id",
                table: "appointment_field_values",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_schedule_unavailability_service_id",
                table: "service_schedule_unavailability",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_schedules_service_id",
                table: "service_schedules",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_secretaries_secretary_id",
                table: "service_secretaries",
                column: "secretary_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_secretaries_service_id",
                table: "service_secretaries",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_types_name",
                table: "service_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_services_owner_id",
                table: "services",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_service_type_id",
                table: "services",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_slug",
                table: "services",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_field_values");

            migrationBuilder.DropTable(
                name: "service_schedule_unavailability");

            migrationBuilder.DropTable(
                name: "service_schedules");

            migrationBuilder.DropTable(
                name: "service_secretaries");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
