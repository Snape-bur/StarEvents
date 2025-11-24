using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarEvents.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoAndLoyaltyFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Discounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PointsDiscountAmount",
                table: "Bookings",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_EventId",
                table: "Discounts",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discounts_Events_EventId",
                table: "Discounts",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discounts_Events_EventId",
                table: "Discounts");

            migrationBuilder.DropIndex(
                name: "IX_Discounts_EventId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "PointsDiscountAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                table: "Bookings");
        }
    }
}
