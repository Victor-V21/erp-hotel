using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintLogoHeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintLogoHeight",
                table: "BusinessSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintLogoHeight",
                table: "BusinessSettings");
        }
    }
}
