using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintPrinterName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrintPrinterName",
                table: "BusinessSettings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintPrinterName",
                table: "BusinessSettings");
        }
    }
}

