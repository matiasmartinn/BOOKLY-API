using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeVisualIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                table: "service_types",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#4E63F5");

            migrationBuilder.AddColumn<string>(
                name: "icon_key",
                table: "service_types",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "service_type_id",
                keyValue: 1,
                columns: new[] { "color_hex", "icon_key" },
                values: new object[] { "#4E63F5", null });

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "service_type_id",
                keyValue: 2,
                columns: new[] { "color_hex", "icon_key" },
                values: new object[] { "#4E63F5", null });

            migrationBuilder.UpdateData(
                table: "service_types",
                keyColumn: "service_type_id",
                keyValue: 3,
                columns: new[] { "color_hex", "icon_key" },
                values: new object[] { "#4E63F5", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color_hex",
                table: "service_types");

            migrationBuilder.DropColumn(
                name: "icon_key",
                table: "service_types");
        }
    }
}
