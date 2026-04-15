using System;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    [DbContext(typeof(BooklyDbContext))]
    [Migration("20260414203000_ReplaceServicePublicBookingTokenWithCode")]
    public partial class ReplaceServicePublicBookingTokenWithCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_services_public_booking_token",
                table: "services");

            migrationBuilder.AddColumn<string>(
                name: "public_booking_code",
                table: "services",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "public_booking_code_updated_at",
                table: "services",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                """
                DECLARE @alphabet nvarchar(62) = N'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
                DECLARE @serviceId int;
                DECLARE @publicBookingCode nvarchar(8);
                DECLARE @position int;

                DECLARE service_cursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT [service_id]
                    FROM [services];

                OPEN service_cursor;
                FETCH NEXT FROM service_cursor INTO @serviceId;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @publicBookingCode = N'';

                    WHILE @publicBookingCode = N'' OR EXISTS (
                        SELECT 1
                        FROM [services]
                        WHERE [public_booking_code] = @publicBookingCode)
                    BEGIN
                        SET @publicBookingCode = N'';
                        SET @position = 0;

                        WHILE @position < 8
                        BEGIN
                            SET @publicBookingCode = @publicBookingCode +
                                SUBSTRING(@alphabet, ABS(CHECKSUM(NEWID())) % LEN(@alphabet) + 1, 1);
                            SET @position = @position + 1;
                        END
                    END

                    UPDATE [services]
                    SET
                        [public_booking_code] = @publicBookingCode,
                        [public_booking_code_updated_at] = COALESCE([public_booking_token_update_at], [created_at], GETDATE())
                    WHERE [service_id] = @serviceId;

                    FETCH NEXT FROM service_cursor INTO @serviceId;
                END

                CLOSE service_cursor;
                DEALLOCATE service_cursor;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "public_booking_code",
                table: "services",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_services_public_booking_code",
                table: "services",
                column: "public_booking_code",
                unique: true);

            migrationBuilder.DropColumn(
                name: "public_booking_token",
                table: "services");

            migrationBuilder.DropColumn(
                name: "public_booking_token_update_at",
                table: "services");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_services_public_booking_code",
                table: "services");

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
                    [public_booking_token_update_at] = COALESCE([public_booking_code_updated_at], [created_at], GETDATE())
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

            migrationBuilder.DropColumn(
                name: "public_booking_code",
                table: "services");

            migrationBuilder.DropColumn(
                name: "public_booking_code_updated_at",
                table: "services");
        }
    }
}
