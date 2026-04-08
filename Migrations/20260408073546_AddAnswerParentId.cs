using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniKnowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerParentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Answers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ParentId",
                table: "Answers",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_Answers_ParentId",
                table: "Answers",
                column: "ParentId",
                principalTable: "Answers",
                principalColumn: "AnswerId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_Answers_ParentId",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_ParentId",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Answers");
        }
    }
}
