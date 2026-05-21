using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFontAndSpacing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrintFontSize",
                table: "BusinessSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PrintLineSpacing",
                table: "BusinessSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintFontSize",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "PrintLineSpacing",
                table: "BusinessSettings");
        }
    }
}

