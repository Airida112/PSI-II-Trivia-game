using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameAdminField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CanEditTrivias",
                table: "Users",
                newName: "CanManageContent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CanManageContent",
                table: "Users",
                newName: "CanEditTrivias");
        }
    }
}
