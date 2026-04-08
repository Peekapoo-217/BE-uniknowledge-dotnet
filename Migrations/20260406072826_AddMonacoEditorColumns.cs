using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniKnowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddMonacoEditorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeContent",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeLanguage",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeContent",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeLanguage",
                table: "Answers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeContent",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CodeLanguage",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CodeContent",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "CodeLanguage",
                table: "Answers");
        }
    }
}
