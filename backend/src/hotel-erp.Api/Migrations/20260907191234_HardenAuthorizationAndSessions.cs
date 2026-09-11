using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hotel_erp.Api.Migrations
{
    /// <inheritdoc />
    public partial class HardenAuthorizationAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_Name",
                table: "Roles");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SecurityVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Roles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT UPPER(BTRIM("Name"))
                        FROM "Roles"
                        GROUP BY UPPER(BTRIM("Name"))
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Existen roles duplicados al ignorar mayúsculas y espacios; deben reconciliarse antes de migrar.';
                    END IF;
                END $$;

                UPDATE "Roles"
                SET "NormalizedName" = UPPER(BTRIM("Name")),
                    "SystemKey" = CASE
                        WHEN UPPER(BTRIM("Name")) = 'ADMIN' THEN 'administrator'
                        WHEN UPPER(BTRIM("Name")) IN ('RECEPCION', 'RECEPCIÓN') THEN 'reception'
                        ELSE NULL
                    END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_SystemKey",
                table: "Roles",
                column: "SystemKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_SystemKey",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "Roles");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);
        }
    }
}
