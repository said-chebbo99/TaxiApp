using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetInfo.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewFieldsOrWhatever : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DistanceKm",
                table: "Rides",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackDate",
                table: "RideFeedbacks",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Drivers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Rides");

            migrationBuilder.DropColumn(
                name: "FeedbackDate",
                table: "RideFeedbacks");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Drivers");
        }
    }
}
