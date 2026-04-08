using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniKnowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeLineCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodeLineCount",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CodeLineCount",
                table: "Answers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeLineCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CodeLineCount",
                table: "Answers");
        }
    }
}
