using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase7Lookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    dial_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    symbol = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "provinces",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provinces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    province_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_districts", x => x.id);
                    table.ForeignKey(
                        name: "fk_districts_provinces_province_id",
                        column: x => x.province_id,
                        principalTable: "provinces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "countries",
                columns: new[] { "code", "dial_code", "is_active", "name" },
                values: new object[,]
                {
                    { "DE", "+49", true, "Germany" },
                    { "FR", "+33", true, "France" },
                    { "GB", "+44", true, "United Kingdom" },
                    { "TR", "+90", true, "Türkiye" },
                    { "US", "+1", true, "United States" }
                });

            migrationBuilder.InsertData(
                table: "currencies",
                columns: new[] { "code", "is_active", "name", "symbol" },
                values: new object[,]
                {
                    { "EUR", true, "Euro", "€" },
                    { "GBP", true, "İngiliz Sterlini", "£" },
                    { "TRY", true, "Türk Lirası", "₺" },
                    { "USD", true, "ABD Doları", "$" }
                });

            migrationBuilder.InsertData(
                table: "provinces",
                columns: new[] { "id", "country_code", "is_active", "name" },
                values: new object[,]
                {
                    { 6, "TR", true, "Ankara" },
                    { 16, "TR", true, "Bursa" },
                    { 34, "TR", true, "İstanbul" },
                    { 35, "TR", true, "İzmir" }
                });

            migrationBuilder.InsertData(
                table: "districts",
                columns: new[] { "id", "is_active", "name", "province_id" },
                values: new object[,]
                {
                    { 601, true, "Çankaya", 6 },
                    { 602, true, "Keçiören", 6 },
                    { 1601, true, "Osmangazi", 16 },
                    { 3401, true, "Kadıköy", 34 },
                    { 3402, true, "Beşiktaş", 34 },
                    { 3403, true, "Şişli", 34 },
                    { 3501, true, "Konak", 35 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_districts_province_id",
                table: "districts",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "ix_provinces_country_code",
                table: "provinces",
                column: "country_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "provinces");
        }
    }
}
