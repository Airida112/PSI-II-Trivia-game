using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClanBadField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberIds",
                table: "Clans");

            migrationBuilder.AddColumn<int>(
                name: "MemberCount",
                table: "Clans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberCount",
                table: "Clans");

            migrationBuilder.AddColumn<List<string>>(
                name: "MemberIds",
                table: "Clans",
                type: "text[]",
                nullable: false);
        }
    }
}
