using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCatalogImperialUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalRangeImperial",
                table: "TestCatalogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitImperial",
                table: "TestCatalogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Instructions",
                table: "DrugCatalogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Route",
                table: "DrugCatalogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 1,
                columns: new[] { "NormalRange", "NormalRangeImperial", "TestUnit", "UnitImperial" },
                values: new object[] { "4.5–11.0", "4500–11000", "×10⁹/L", "cells/µL" });

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 2,
                columns: new[] { "NormalRange", "NormalRangeImperial", "TestUnit", "UnitImperial" },
                values: new object[] { "3.9–6.1", "70–110", "mmol/L", "mg/dL" });

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 3,
                columns: new[] { "NormalRange", "NormalRangeImperial", "TestUnit", "UnitImperial" },
                values: new object[] { "<5.2", "<200", "mmol/L", "mg/dL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalRangeImperial",
                table: "TestCatalogs");

            migrationBuilder.DropColumn(
                name: "UnitImperial",
                table: "TestCatalogs");

            migrationBuilder.DropColumn(
                name: "Instructions",
                table: "DrugCatalogs");

            migrationBuilder.DropColumn(
                name: "Route",
                table: "DrugCatalogs");

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 1,
                columns: new[] { "NormalRange", "TestUnit" },
                values: new object[] { "4.5-11.0", "cells/µL" });

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 2,
                columns: new[] { "NormalRange", "TestUnit" },
                values: new object[] { "70-100", "mg/dL" });

            migrationBuilder.UpdateData(
                table: "TestCatalogs",
                keyColumn: "TestId",
                keyValue: 3,
                columns: new[] { "NormalRange", "TestUnit" },
                values: new object[] { "<200", "mg/dL" });
        }
    }
}
