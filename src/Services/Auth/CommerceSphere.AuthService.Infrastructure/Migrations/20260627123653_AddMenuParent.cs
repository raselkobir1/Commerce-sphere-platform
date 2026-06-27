using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceSphere.AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                table: "menus",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_menus_parent",
                table: "menus",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_menus_parent",
                table: "menus");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "menus");
        }
    }
}
