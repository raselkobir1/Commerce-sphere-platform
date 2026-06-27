using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceSphere.ProductService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPublished : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing products default to published (true) so the current storefront catalog is
            // unaffected. New products insert with the entity's value (false = draft), overriding this.
            migrationBuilder.AddColumn<bool>(
                name: "is_published",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_published",
                table: "products");
        }
    }
}
