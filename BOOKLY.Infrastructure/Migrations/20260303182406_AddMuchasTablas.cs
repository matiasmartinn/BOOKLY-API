using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMuchasTablas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_services_service_types_service_type_id",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "FK_services_users_owner_id",
                table: "services");

            migrationBuilder.DropIndex(
                name: "IX_appointment_field_values_appointment_id",
                table: "appointment_field_values");

            migrationBuilder.RenameColumn(
                name: "internal_notes",
                table: "appointments",
                newName: "cancel_reason");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "service_types",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_on",
                table: "appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_status_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    appointment_id = table.Column<int>(type: "int", nullable: false),
                    old_status = table.Column<int>(type: "int", nullable: true),
                    new_status = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    occurred_on = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_status_history_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_type_field_definitions",
                columns: table => new
                {
                    field_definition_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    service_type_id = table.Column<int>(type: "int", nullable: false),
                    key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    field_type = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_type_field_definitions", x => x.field_definition_id);
                    table.ForeignKey(
                        name: "FK_service_type_field_definitions_service_types_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "service_types",
                        principalColumn: "service_type_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: true),
                    plan_name = table.Column<int>(type: "int", nullable: false),
                    max_services = table.Column<int>(type: "int", nullable: false),
                    max_secretaries = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_on = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.id);
                    table.CheckConstraint("ck_subscriptions_free_end_date", "[plan_name] <> 1 OR [end_date] IS NULL");
                    table.CheckConstraint("ck_subscriptions_period_dates", "[end_date] IS NULL OR [end_date] >= [start_date]");
                });

            migrationBuilder.CreateTable(
                name: "service_type_field_options",
                columns: table => new
                {
                    field_option_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    field_definition_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_type_field_options", x => x.field_option_id);
                    table.ForeignKey(
                        name: "FK_service_type_field_options_service_type_field_definitions_field_definition_id",
                        column: x => x.field_definition_id,
                        principalTable: "service_type_field_definitions",
                        principalColumn: "field_definition_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_field_values_field_definition_id",
                table: "appointment_field_values",
                column: "field_definition_id");

            migrationBuilder.CreateIndex(
                name: "ux_appointment_field_values_appointment_field",
                table: "appointment_field_values",
                columns: new[] { "appointment_id", "field_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointment_status_history_appointment_id",
                table: "appointment_status_history",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_type_field_definitions_service_type_sort",
                table: "service_type_field_definitions",
                columns: new[] { "service_type_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_service_type_field_options_definition_sort",
                table: "service_type_field_options",
                columns: new[] { "field_definition_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_service_type_field_options_definition_value",
                table: "service_type_field_options",
                columns: new[] { "field_definition_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_user_id",
                table: "subscriptions",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_appointment_field_values_service_type_field_definitions_field_definition_id",
                table: "appointment_field_values",
                column: "field_definition_id",
                principalTable: "service_type_field_definitions",
                principalColumn: "field_definition_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointment_field_values_service_type_field_definitions_field_definition_id",
                table: "appointment_field_values");

            migrationBuilder.DropTable(
                name: "appointment_status_history");

            migrationBuilder.DropTable(
                name: "service_type_field_options");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "service_type_field_definitions");

            migrationBuilder.DropIndex(
                name: "IX_appointment_field_values_field_definition_id",
                table: "appointment_field_values");

            migrationBuilder.DropIndex(
                name: "ux_appointment_field_values_appointment_field",
                table: "appointment_field_values");

            migrationBuilder.DropColumn(
                name: "cancelled_on",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "cancel_reason",
                table: "appointments",
                newName: "internal_notes");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "service_types",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointment_field_values_appointment_id",
                table: "appointment_field_values",
                column: "appointment_id");

            migrationBuilder.AddForeignKey(
                name: "FK_services_service_types_service_type_id",
                table: "services",
                column: "service_type_id",
                principalTable: "service_types",
                principalColumn: "service_type_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_services_users_owner_id",
                table: "services",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
