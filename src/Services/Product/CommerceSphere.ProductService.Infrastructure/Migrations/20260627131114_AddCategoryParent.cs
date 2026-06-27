using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceSphere.ProductService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                table: "categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent",
                table: "categories",
                column: "parent_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_parent",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "parent_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "categories");
        }
    }
}
