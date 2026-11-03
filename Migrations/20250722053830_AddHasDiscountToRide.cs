using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetInfo.Migrations
{
    /// <inheritdoc />
    public partial class AddHasDiscountToRide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasDiscount",
                table: "Rides",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasDiscount",
                table: "Rides");
        }
    }
}
