using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasVehicle",
                table: "Guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RTN",
                table: "Guests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePlate",
                table: "Guests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Company",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "HasVehicle",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "RTN",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "VehiclePlate",
                table: "Guests");
        }
    }
}

