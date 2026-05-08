using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_types",
                columns: table => new
                {
                    service_type_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#4E63F5"),
                    icon_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_types", x => x.service_type_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "service_type_field_definitions",
                columns: table => new
                {
                    field_definition_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_type_id = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    field_type = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                name: "RefreshTokens",
                columns: table => new
                {
                    refresh_token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.refresh_token_id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    place_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    service_type_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_public_booking_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    public_booking_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    public_booking_code_updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
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
                name: "subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "date", nullable: false),
                    end_date = table.Column<DateTime>(type: "date", nullable: true),
                    plan_name = table.Column<int>(type: "integer", nullable: false),
                    max_services = table.Column<int>(type: "integer", nullable: false),
                    max_secretaries = table.Column<int>(type: "integer", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.subscription_id);
                    table.CheckConstraint("ck_subscriptions_free_end_date", "plan_name <> 1 OR end_date IS NULL");
                    table.CheckConstraint("ck_subscriptions_period_dates", "end_date IS NULL OR end_date >= start_date");
                    table.ForeignKey(
                        name: "FK_subscriptions_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    user_token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_on = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => x.user_token_id);
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_type_field_options",
                columns: table => new
                {
                    field_option_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    field_definition_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_type_field_options", x => x.field_option_id);
                    table.ForeignKey(
                        name: "FK_service_type_field_options_service_type_field_definitions_f~",
                        column: x => x.field_definition_id,
                        principalTable: "service_type_field_definitions",
                        principalColumn: "field_definition_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    appointment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_secretary_id = table.Column<int>(type: "integer", nullable: true),
                    client_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    client_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    start_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    client_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancel_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancelled_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.appointment_id);
                    table.ForeignKey(
                        name: "FK_appointments_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_appointments_users_assigned_secretary_id",
                        column: x => x.assigned_secretary_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_appointments_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "service_schedules",
                columns: table => new
                {
                    service_schedule_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    day = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false)
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
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    secretary_id = table.Column<int>(type: "integer", nullable: false),
                    permissions = table.Column<string>(type: "text", nullable: false, defaultValue: "[]")
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

            migrationBuilder.CreateTable(
                name: "service_unavailabilities",
                columns: table => new
                {
                    service_unavailability_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    reason = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "appointment_field_values",
                columns: table => new
                {
                    appointment_field_value_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    field_definition_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_field_values", x => x.appointment_field_value_id);
                    table.ForeignKey(
                        name: "FK_appointment_field_values_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_field_values_service_type_field_definitions_fie~",
                        column: x => x.field_definition_id,
                        principalTable: "service_type_field_definitions",
                        principalColumn: "field_definition_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "appointment_status_history",
                columns: table => new
                {
                    appointment_status_history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointment_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    old_status = table.Column<int>(type: "integer", nullable: true),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    occurred_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_status_history", x => x.appointment_status_history_id);
                    table.ForeignKey(
                        name: "FK_appointment_status_history_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "appointment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_status_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
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
                name: "ix_appointment_status_history_user_id",
                table: "appointment_status_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_assigned_secretary_id",
                table: "appointments",
                column: "assigned_secretary_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_service_id",
                table: "appointments",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_updated_by",
                table: "appointments",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "RefreshTokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "RefreshTokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token",
                table: "RefreshTokens",
                column: "token",
                unique: true);

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
                name: "ix_service_types_name",
                table: "service_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_unavailabilities_service_id",
                table: "service_unavailabilities",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_created_at",
                table: "services",
                column: "created_at");

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
                name: "ux_services_public_booking_code",
                table: "services",
                column: "public_booking_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_subscriptions_owner_id",
                table: "subscriptions",
                column: "owner_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_expires_on",
                table: "user_tokens",
                column: "expires_on");

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_token_hash",
                table: "user_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_used_on",
                table: "user_tokens",
                column: "used_on");

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_id",
                table: "user_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_id_purpose",
                table: "user_tokens",
                columns: new[] { "user_id", "purpose" });

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
                name: "ix_users_email_confirmed",
                table: "users",
                column: "email_confirmed");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointment_field_values");

            migrationBuilder.DropTable(
                name: "appointment_status_history");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "service_schedules");

            migrationBuilder.DropTable(
                name: "service_secretaries");

            migrationBuilder.DropTable(
                name: "service_type_field_options");

            migrationBuilder.DropTable(
                name: "service_unavailabilities");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "service_type_field_definitions");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "service_types");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
