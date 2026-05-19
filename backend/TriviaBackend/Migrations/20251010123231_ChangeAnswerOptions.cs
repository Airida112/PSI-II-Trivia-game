using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAnswerOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Options",
                table: "Questions",
                newName: "Answer4");

            migrationBuilder.AddColumn<string>(
                name: "Answer1",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Answer2",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Answer3",
                table: "Questions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 100,
                columns: new[] { "Answer1", "Answer2", "Answer3", "Answer4" },
                values: new object[] { "Paris", "Tokyo", "Shanghai", "Gelgaudiškis" });

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 200,
                columns: new[] { "Answer1", "Answer2", "Answer3", "Answer4" },
                values: new object[] { "pi", "e", "golden ratio", "square root of 2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Answer1",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Answer2",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Answer3",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "Answer4",
                table: "Questions",
                newName: "Options");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 100,
                column: "Options",
                value: "[\"Paris\",\"Tokyo\",\"Shanghai\",\"Gelgaudi\\u0161kis\"]");

            migrationBuilder.UpdateData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 200,
                column: "Options",
                value: "[\"pi\",\"e\",\"golden ratio\",\"square root of 2\"]");
        }
    }
}
