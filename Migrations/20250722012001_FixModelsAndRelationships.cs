using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetInfo.Migrations
{
    /// <inheritdoc />
    public partial class FixModelsAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Rides_RideID",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_RideID",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Contacts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "ChatMessages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RideID_Timestamp",
                table: "ChatMessages",
                columns: new[] { "RideID", "Timestamp" });

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Rides_RideID",
                table: "ChatMessages",
                column: "RideID",
                principalTable: "Rides",
                principalColumn: "RideID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Rides_RideID",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_RideID_Timestamp",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmittedAt",
                table: "Contacts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "ChatMessages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSDATETIME()");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_RideID",
                table: "ChatMessages",
                column: "RideID");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Rides_RideID",
                table: "ChatMessages",
                column: "RideID",
                principalTable: "Rides",
                principalColumn: "RideID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
