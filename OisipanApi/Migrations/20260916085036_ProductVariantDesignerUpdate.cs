using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class ProductVariantDesignerUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesignData",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDesignerEnabled",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesignData",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsDesignerEnabled",
                table: "ProductVariants");
        }
    }
}
