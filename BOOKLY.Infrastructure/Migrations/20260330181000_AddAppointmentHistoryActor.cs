using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentHistoryActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "appointment_status_history",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_status_history_user_id",
                table: "appointment_status_history",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_appointment_status_history_users_user_id",
                table: "appointment_status_history",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_appointment_status_history_users_user_id",
                table: "appointment_status_history");

            migrationBuilder.DropIndex(
                name: "ix_appointment_status_history_user_id",
                table: "appointment_status_history");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "appointment_status_history");
        }
    }
}
