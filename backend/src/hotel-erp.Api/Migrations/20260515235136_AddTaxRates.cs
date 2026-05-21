using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IsvRate",
                table: "BusinessSettings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TouristTaxRate",
                table: "BusinessSettings",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsvRate",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "TouristTaxRate",
                table: "BusinessSettings");
        }
    }
}

