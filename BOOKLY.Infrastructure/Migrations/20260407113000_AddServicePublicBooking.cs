using System;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    [DbContext(typeof(BooklyDbContext))]
    [Migration("20260407113000_AddServicePublicBooking")]
    public partial class AddServicePublicBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_public_booking_enabled",
                table: "services",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "public_booking_token",
                table: "services",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "public_booking_token_update_at",
                table: "services",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [services]
                SET
                    [public_booking_token] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')),
                    [public_booking_token_update_at] = COALESCE([created_at], GETDATE())
                WHERE [public_booking_token] IS NULL OR LTRIM(RTRIM([public_booking_token])) = ''
                """);

            migrationBuilder.AlterColumn<string>(
                name: "public_booking_token",
                table: "services",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_services_public_booking_token",
                table: "services",
                column: "public_booking_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_services_public_booking_token",
                table: "services");

            migrationBuilder.DropColumn(
                name: "is_public_booking_enabled",
                table: "services");

            migrationBuilder.DropColumn(
                name: "public_booking_token",
                table: "services");

            migrationBuilder.DropColumn(
                name: "public_booking_token_update_at",
                table: "services");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
        }
    }
}
