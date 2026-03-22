using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatellitePortfolio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNonCashAcquisitionTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CostBasisMode",
                table: "trades",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomTotalCost",
                table: "trades",
                type: "numeric(20,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostBasisMode",
                table: "trades");

            migrationBuilder.DropColumn(
                name: "CustomTotalCost",
                table: "trades");
        }
    }
}
