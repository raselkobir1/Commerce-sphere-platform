using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceSphere.AuthService.Infrastructure.Migrations
{
    // Adds the external_logins table that links local User accounts to social identities
    // from Keycloak-brokered providers (Google, GitHub, Facebook, Twitter).
    public partial class AddExternalLogins : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_logins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    // Lowercase provider alias matching the Keycloak identity provider alias
                    // (e.g. "google", "github", "facebook", "twitter").
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    // The user's unique ID from Keycloak (sub claim of the id_token).
                    external_user_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_logins", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Unique index: one (provider, externalUserId) pair can only belong to one local user.
            migrationBuilder.CreateIndex(
                name: "ix_external_logins_provider_external_user_id",
                table: "external_logins",
                columns: new[] { "provider", "external_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_logins_user_id",
                table: "external_logins",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "external_logins");
        }
    }
}
