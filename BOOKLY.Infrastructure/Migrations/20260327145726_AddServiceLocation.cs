using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "services",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "google_maps_url",
                table: "services",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "place_name",
                table: "services",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address",
                table: "services");

            migrationBuilder.DropColumn(
                name: "google_maps_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "place_name",
                table: "services");
        }
    }
}
