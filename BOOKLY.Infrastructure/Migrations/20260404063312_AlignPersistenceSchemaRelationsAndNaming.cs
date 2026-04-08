using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignPersistenceSchemaRelationsAndNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "subscriptions",
                newName: "owner_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "subscriptions",
                newName: "subscription_id");

            migrationBuilder.RenameIndex(
                name: "ux_subscriptions_user_id",
                table: "subscriptions",
                newName: "ux_subscriptions_owner_id");

            migrationBuilder.RenameColumn(
                name: "update_on",
                table: "appointments",
                newName: "updated_on");

            migrationBuilder.RenameColumn(
                name: "update_by",
                table: "appointments",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "appointments",
                newName: "appointment_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "appointment_status_history",
                newName: "appointment_status_history_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "appointment_field_values",
                newName: "appointment_field_value_id");

            migrationBuilder.Sql(
                """
                -- Preserve legacy services/subscriptions whose owner row was deleted.
                -- These relationships are required by the domain, so we recreate a minimal
                -- inactive Owner user instead of nulling the FK or dropping historical data.
                DECLARE @MissingOwners TABLE ([user_id] INT PRIMARY KEY);

                INSERT INTO @MissingOwners ([user_id])
                SELECT DISTINCT [ref].[user_id]
                FROM
                (
                    SELECT [owner_id] AS [user_id] FROM [services]
                    UNION
                    SELECT [owner_id] AS [user_id] FROM [subscriptions]
                ) AS [ref]
                LEFT JOIN [users] AS [u] ON [u].[user_id] = [ref].[user_id]
                WHERE [ref].[user_id] IS NOT NULL
                  AND [u].[user_id] IS NULL;

                IF EXISTS (SELECT 1 FROM @MissingOwners)
                BEGIN
                    SET IDENTITY_INSERT [users] ON;

                    INSERT INTO [users]
                    (
                        [user_id],
                        [first_name],
                        [last_name],
                        [email],
                        [password_hash],
                        [role],
                        [is_active],
                        [email_confirmed],
                        [created_at],
                        [last_login_at]
                    )
                    SELECT
                        [user_id],
                        'Migrated',
                        CONCAT('Legacy Owner ', [user_id]),
                        CONCAT('legacy-owner-', [user_id], '@bookly.local'),
                        NULL,
                        'Owner',
                        CAST(0 AS bit),
                        CAST(0 AS bit),
                        GETDATE(),
                        NULL
                    FROM @MissingOwners;

                    SET IDENTITY_INSERT [users] OFF;
                END
                """);

            migrationBuilder.Sql(
                """
                UPDATE [appointments]
                SET [assigned_secretary_id] = NULL
                WHERE [assigned_secretary_id] IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [users]
                      WHERE [user_id] = [appointments].[assigned_secretary_id])
                """);

            migrationBuilder.Sql(
                """
                UPDATE [appointments]
                SET [updated_by] = NULL
                WHERE [updated_by] IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [users]
                      WHERE [user_id] = [appointments].[updated_by])
                """);

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

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_services_service_id",
                table: "appointments",
                column: "service_id",
                principalTable: "services",
                principalColumn: "service_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_assigned_secretary_id",
                table: "appointments",
                column: "assigned_secretary_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_users_updated_by",
                table: "appointments",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.NoAction);

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

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_users_owner_id",
                table: "subscriptions",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_services_service_id",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_assigned_secretary_id",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_users_updated_by",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_services_service_types_service_type_id",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "FK_services_users_owner_id",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_users_owner_id",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_appointments_assigned_secretary_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_service_id",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_updated_by",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "subscriptions",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "subscription_id",
                table: "subscriptions",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "ux_subscriptions_owner_id",
                table: "subscriptions",
                newName: "ux_subscriptions_user_id");

            migrationBuilder.RenameColumn(
                name: "updated_on",
                table: "appointments",
                newName: "update_on");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "appointments",
                newName: "update_by");

            migrationBuilder.RenameColumn(
                name: "appointment_id",
                table: "appointments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "appointment_status_history_id",
                table: "appointment_status_history",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "appointment_field_value_id",
                table: "appointment_field_values",
                newName: "id");
        }
    }
}
