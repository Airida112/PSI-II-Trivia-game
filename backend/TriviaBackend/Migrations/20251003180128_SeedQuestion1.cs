using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class SeedQuestion1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Options",
                table: "Questions",
                type: "text",
                nullable: false,
                oldClrType: typeof(List<string>),
                oldType: "text[]");

            migrationBuilder.InsertData(
                table: "Questions",
                columns: new[] { "Id", "Category", "CorrectAnswerIndex", "Difficulty", "Options", "QuestionText", "TimeLimit" },
                values: new object[,]
                {
                    { 100, 3, 1, 1, "[\"Paris\",\"Tokyo\",\"Shanghai\",\"Gelgaudi\\u0161kis\"]", "What is the most populated city?", 20 },
                    { 200, 3, 3, 2, "[\"pi\",\"e\",\"golden ratio\",\"square root of 2\"]", "which is least", 30 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Questions",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.AlterColumn<List<string>>(
                name: "Options",
                table: "Questions",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
