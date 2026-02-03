using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMate_app.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProfilesOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MechanicProfiles_UserId",
                table: "MechanicProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MechanicProfiles_UserId",
                table: "MechanicProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MechanicProfiles_UserId",
                table: "MechanicProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanicProfiles_UserId",
                table: "MechanicProfiles",
                column: "UserId");
        }
    }
}
