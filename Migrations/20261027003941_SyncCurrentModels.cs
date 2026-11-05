using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WellnessPlatform.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_WellnessRecords_ClientId",
                table: "WellnessRecords",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId_PermissionId",
                table: "UserPermissions",
                columns: new[] { "UserId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Goals_ClientId",
                table: "Goals",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Goals_Users_ClientId",
                table: "Goals",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WellnessRecords_Users_ClientId",
                table: "WellnessRecords",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Goals_Users_ClientId",
                table: "Goals");

            migrationBuilder.DropForeignKey(
                name: "FK_WellnessRecords_Users_ClientId",
                table: "WellnessRecords");

            migrationBuilder.DropIndex(
                name: "IX_WellnessRecords_ClientId",
                table: "WellnessRecords");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_UserId_PermissionId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_Goals_ClientId",
                table: "Goals");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");
        }
    }
}
