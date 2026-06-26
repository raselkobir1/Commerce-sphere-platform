using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceSphere.AuthService.Infrastructure.Migrations
{
    public partial class AddSecurityFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>("email_verified", "users", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>("email_verification_token", "users", maxLength: 128, nullable: true);
            migrationBuilder.AddColumn<DateTime>("email_verification_token_expiry", "users", nullable: true);

            migrationBuilder.AddColumn<bool>("is_active_two_factor", "users", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>("two_factor_secret", "users", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<bool>("two_factor_confirmed", "users", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<bool>("is_otp_auth_enable", "users", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>("password_reset_token", "users", maxLength: 128, nullable: true);
            migrationBuilder.AddColumn<DateTime>("password_reset_token_expiry", "users", nullable: true);

            migrationBuilder.AddColumn<int>("failed_login_attempts", "users", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<DateTime>("lockout_end", "users", nullable: true);

            migrationBuilder.CreateIndex("ix_users_email_verification_token", "users", "email_verification_token");
            migrationBuilder.CreateIndex("ix_users_password_reset_token", "users", "password_reset_token");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("ix_users_email_verification_token", "users");
            migrationBuilder.DropIndex("ix_users_password_reset_token", "users");
            migrationBuilder.DropColumn("email_verified", "users");
            migrationBuilder.DropColumn("email_verification_token", "users");
            migrationBuilder.DropColumn("email_verification_token_expiry", "users");
            migrationBuilder.DropColumn("is_active_two_factor", "users");
            migrationBuilder.DropColumn("two_factor_secret", "users");
            migrationBuilder.DropColumn("two_factor_confirmed", "users");
            migrationBuilder.DropColumn("is_otp_auth_enable", "users");
            migrationBuilder.DropColumn("password_reset_token", "users");
            migrationBuilder.DropColumn("password_reset_token_expiry", "users");
            migrationBuilder.DropColumn("failed_login_attempts", "users");
            migrationBuilder.DropColumn("lockout_end", "users");
        }
    }
}
