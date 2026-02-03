using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMate_app.Data.Migrations
{
    /// <inheritdoc />
    public partial class AiProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AiCalculatedAt",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiPossibleReasonsJson",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiRecommendTowing",
                table: "ServiceRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSuggestedServiceType",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiUrgency",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCalculatedAt",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AiPossibleReasonsJson",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AiRecommendTowing",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AiSuggestedServiceType",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AiUrgency",
                table: "ServiceRequests");
        }
    }
}
