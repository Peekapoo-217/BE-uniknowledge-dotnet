using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniKnowledge.Migrations
{
    /// <inheritdoc />
    public partial class AddCursorPaginationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_UserId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_SenderId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CreatedAt_Id",
                table: "Questions",
                columns: new[] { "CreatedAt", "QuestionId" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId_CreatedAt_Id",
                table: "Questions",
                columns: new[] { "UserId", "CreatedAt", "QuestionId" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Sender_Receiver_CreatedAt",
                table: "Messages",
                columns: new[] { "SenderId", "ReceiverId", "CreatedAt", "MessageId" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Unread_Lookup",
                table: "Messages",
                columns: new[] { "ReceiverId", "IsRead", "SenderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId_Cursor",
                table: "Answers",
                columns: new[] { "QuestionId", "IsAccepted", "CreatedAt", "AnswerId" },
                descending: new[] { false, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_CreatedAt_Id",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_UserId_CreatedAt_Id",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Sender_Receiver_CreatedAt",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Unread_Lookup",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Answers_QuestionId_Cursor",
                table: "Answers");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_UserId",
                table: "Questions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId",
                table: "Answers",
                column: "QuestionId");
        }
    }
}
