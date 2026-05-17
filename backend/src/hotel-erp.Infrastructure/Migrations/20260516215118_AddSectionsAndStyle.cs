using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionsAndStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeaderAlign",
                table: "BusinessSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MarginLeft",
                table: "BusinessSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SeparatorChar",
                table: "BusinessSettings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowFiscal",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowFooter",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowGuest",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeader",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowItems",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowLogo",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPayment",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTotals",
                table: "BusinessSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeaderAlign",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "MarginLeft",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "SeparatorChar",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowFiscal",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowFooter",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowGuest",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowHeader",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowItems",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowLogo",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowPayment",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ShowTotals",
                table: "BusinessSettings");
        }
    }
}
