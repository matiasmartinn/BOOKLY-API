using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "users",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [users]
                SET [status] = CASE
                    WHEN [is_active] = 0 THEN 4
                    WHEN [role] = 'Owner' AND [email_confirmed] = 0 THEN 1
                    WHEN [password_hash] IS NULL THEN 2
                    WHEN [email_confirmed] = 1 AND [password_hash] IS NOT NULL THEN 3
                    ELSE 4
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                table: "users",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "users");
        }
    }
}
