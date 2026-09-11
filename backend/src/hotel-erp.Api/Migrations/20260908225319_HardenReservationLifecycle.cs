using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Reservations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(
                """
                UPDATE "Rooms" AS room
                SET "Status" = 'Ocupada'
                WHERE room."Status" = 'Reservada'
                  AND EXISTS (
                      SELECT 1
                      FROM "Reservations" AS reservation
                      WHERE reservation."RoomId" = room."Id"
                        AND reservation."Status" = 'CheckIn'
                        AND NOT reservation."IsDeleted");

                UPDATE "Rooms"
                SET "Status" = 'Libre'
                WHERE "Status" = 'Reservada';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Reservations");
        }
    }
}
