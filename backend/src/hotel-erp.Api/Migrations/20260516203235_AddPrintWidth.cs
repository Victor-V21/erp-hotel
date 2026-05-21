using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintWidth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintWidth",
                table: "BusinessSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrintWidth",
                table: "BusinessSettings");
        }
    }
}

