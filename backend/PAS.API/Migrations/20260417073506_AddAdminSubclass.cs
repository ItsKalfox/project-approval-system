using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PAS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSubclass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_ProjectId",
                table: "Matches");

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Admins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ProjectId",
                table: "Matches",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropIndex(
                name: "IX_Matches_ProjectId",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ProjectId",
                table: "Matches",
                column: "ProjectId");
        }
    }
}
