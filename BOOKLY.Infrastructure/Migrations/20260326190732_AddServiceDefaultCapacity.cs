using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDefaultCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "capacity",
                table: "services",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE services
                SET capacity = 1
                WHERE capacity <= 0
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capacity",
                table: "services");
        }
    }
}
