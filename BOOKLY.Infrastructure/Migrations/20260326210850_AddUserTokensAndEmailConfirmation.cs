using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOKLY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTokensAndEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_invitations_users_user_id",
                table: "user_invitations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_invitations",
                table: "user_invitations");

            migrationBuilder.RenameTable(
                name: "user_invitations",
                newName: "user_tokens");

            migrationBuilder.RenameColumn(
                name: "user_invitation_id",
                table: "user_tokens",
                newName: "user_token_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_invitations_expires_on",
                table: "user_tokens",
                newName: "ix_user_tokens_expires_on");

            migrationBuilder.RenameIndex(
                name: "ix_user_invitations_token_hash",
                table: "user_tokens",
                newName: "ix_user_tokens_token_hash");

            migrationBuilder.RenameIndex(
                name: "ix_user_invitations_used_on",
                table: "user_tokens",
                newName: "ix_user_tokens_used_on");

            migrationBuilder.RenameIndex(
                name: "ix_user_invitations_user_id",
                table: "user_tokens",
                newName: "ix_user_tokens_user_id");

            migrationBuilder.AddColumn<bool>(
                name: "email_confirmed",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "user_tokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "SecretaryInvitation");

            migrationBuilder.Sql(
                """
                UPDATE users
                SET email_confirmed = CASE
                    WHEN is_active = 1 THEN 1
                    ELSE 0
                END
                """);

            migrationBuilder.CreateIndex(
                name: "ix_users_email_confirmed",
                table: "users",
                column: "email_confirmed");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_tokens",
                table: "user_tokens",
                column: "user_token_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_tokens_users_user_id",
                table: "user_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "ix_user_tokens_user_id_purpose",
                table: "user_tokens",
                columns: new[] { "user_id", "purpose" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_tokens_users_user_id",
                table: "user_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_tokens",
                table: "user_tokens");

            migrationBuilder.DropIndex(
                name: "ix_user_tokens_user_id_purpose",
                table: "user_tokens");

            migrationBuilder.DropIndex(
                name: "ix_users_email_confirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email_confirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "purpose",
                table: "user_tokens");

            migrationBuilder.RenameColumn(
                name: "user_token_id",
                table: "user_tokens",
                newName: "user_invitation_id");

            migrationBuilder.RenameTable(
                name: "user_tokens",
                newName: "user_invitations");

            migrationBuilder.RenameIndex(
                name: "ix_user_tokens_expires_on",
                table: "user_invitations",
                newName: "ix_user_invitations_expires_on");

            migrationBuilder.RenameIndex(
                name: "ix_user_tokens_token_hash",
                table: "user_invitations",
                newName: "ix_user_invitations_token_hash");

            migrationBuilder.RenameIndex(
                name: "ix_user_tokens_used_on",
                table: "user_invitations",
                newName: "ix_user_invitations_used_on");

            migrationBuilder.RenameIndex(
                name: "ix_user_tokens_user_id",
                table: "user_invitations",
                newName: "ix_user_invitations_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_invitations",
                table: "user_invitations",
                column: "user_invitation_id");

            migrationBuilder.AddForeignKey(
                name: "FK_user_invitations_users_user_id",
                table: "user_invitations",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
