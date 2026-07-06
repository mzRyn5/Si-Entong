using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Store.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiAssistantChatbotTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_action_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    request_payload = table.Column<string>(type: "text", nullable: false),
                    response_payload = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_action_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_chat_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_active_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chat_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_action_drafts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    draft_payload = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    expired_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_action_drafts", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_action_drafts_ai_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "ai_chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    intent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tool_calls = table.Column<string>(type: "text", nullable: true),
                    tool_results = table.Column<string>(type: "text", nullable: true),
                    function_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    token_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_chat_messages_ai_chat_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "ai_chat_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_action_drafts_expiry",
                table: "ai_action_drafts",
                column: "expired_at",
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "idx_action_drafts_session_status",
                table: "ai_action_drafts",
                columns: new[] { "session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_action_logs_store_id",
                table: "ai_action_logs",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_messages_session",
                table: "ai_chat_messages",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chat_sessions_store_id",
                table: "ai_chat_sessions",
                column: "store_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_action_drafts");

            migrationBuilder.DropTable(
                name: "ai_action_logs");

            migrationBuilder.DropTable(
                name: "ai_chat_messages");

            migrationBuilder.DropTable(
                name: "ai_chat_sessions");
        }
    }
}
