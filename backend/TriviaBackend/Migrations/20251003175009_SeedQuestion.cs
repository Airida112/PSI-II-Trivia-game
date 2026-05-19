using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class SeedQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "Category", "CorrectAnswerIndex", "Difficulty", "Options", "QuestionText", "TimeLimit" },
                values: new object[] { 1, 3, 1, 1, "[\"Paris\",\"Tokyo\",\"Shanghai\",\"Gelgaudi\\u0161kis\"]", "What is the most populated city?", 20 });
        }
    }
}
