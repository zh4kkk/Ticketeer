using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exams", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_role_group", "(role = 'Admin' AND group_id IS NULL) OR role = 'Cashier'");
                    table.ForeignKey(
                        name: "FK_users_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    topic_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ticket_id = table.Column<int>(type: "integer", nullable: true),
                    exam_id = table.Column<int>(type: "integer", nullable: true),
                    group_id_snapshot = table.Column<int>(type: "integer", nullable: true),
                    group_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    wrong_answers = table.Column<int>(type: "integer", nullable: false),
                    is_passed = table.Column<bool>(type: "boolean", nullable: true),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    time_limit_seconds = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempts", x => x.id);
                    table.CheckConstraint("ck_attempts_completed_passed", "(status <> 'Completed' OR kind <> 'Exam' OR is_passed IS NOT NULL)");
                    table.CheckConstraint("ck_attempts_counters", "correct_answers >= 0 AND wrong_answers >= 0 AND question_count > 0 AND (time_limit_seconds IS NULL OR time_limit_seconds > 0) AND (finished_at IS NULL OR finished_at >= started_at)");
                    table.CheckConstraint("ck_attempts_kind_values", "kind IN ('Training', 'Exam')");
                    table.CheckConstraint("ck_attempts_source_references", "kind IN ('Training', 'Exam') AND (source = 'Random' AND ticket_id IS NULL AND exam_id IS NULL) OR (source = 'Prepared' AND ((kind = 'Training' AND exam_id IS NULL) OR (kind = 'Exam' AND ticket_id IS NULL)))");
                    table.CheckConstraint("ck_attempts_source_values", "source IN ('Prepared', 'Random')");
                    table.CheckConstraint("ck_attempts_status_values", "status IN ('InProgress', 'Completed', 'Failed', 'TimedOut')");
                    table.CheckConstraint("ck_attempts_training_passed", "(kind = 'Training' AND is_passed IS NULL) OR (kind = 'Exam')");
                    table.ForeignKey(
                        name: "FK_attempts_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attempts_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "login_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_login_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "answer_options",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_options", x => x.id);
                    table.CheckConstraint("ck_answer_options_position_positive", "position > 0");
                    table.ForeignKey(
                        name: "FK_answer_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exam_questions",
                columns: table => new
                {
                    exam_id = table.Column<int>(type: "integer", nullable: false),
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam_questions", x => new { x.exam_id, x.question_id });
                    table.CheckConstraint("ck_exam_questions_position_positive", "position > 0");
                    table.ForeignKey(
                        name: "FK_exam_questions_exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "exams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_exam_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_questions",
                columns: table => new
                {
                    ticket_id = table.Column<int>(type: "integer", nullable: false),
                    question_id = table.Column<int>(type: "integer", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_questions", x => new { x.ticket_id, x.question_id });
                    table.CheckConstraint("ck_ticket_questions_position_positive", "position > 0");
                    table.ForeignKey(
                        name: "FK_ticket_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ticket_questions_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempt_questions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attempt_id = table.Column<int>(type: "integer", nullable: false),
                    question_id = table.Column<int>(type: "integer", nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    question_text = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    options_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    selected_option_position = table.Column<int>(type: "integer", nullable: true),
                    is_correct = table.Column<bool>(type: "boolean", nullable: true),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attempt_questions", x => x.id);
                    table.CheckConstraint("ck_attempt_questions_answer_consistency", "(selected_option_position IS NULL AND is_correct IS NULL AND answered_at IS NULL) OR (selected_option_position IS NOT NULL AND selected_option_position > 0 AND is_correct IS NOT NULL AND answered_at IS NOT NULL)");
                    table.CheckConstraint("ck_attempt_questions_position_positive", "position > 0");
                    table.ForeignKey(
                        name: "FK_attempt_questions_attempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "attempts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attempt_questions_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answer_options_question_id",
                table: "answer_options",
                column: "question_id",
                unique: true,
                filter: "is_correct = true");

            migrationBuilder.CreateIndex(
                name: "IX_answer_options_question_id_position",
                table: "answer_options",
                columns: new[] { "question_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attempt_questions_attempt_id_position",
                table: "attempt_questions",
                columns: new[] { "attempt_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attempt_questions_question_id",
                table: "attempt_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_exam_id",
                table: "attempts",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_started_at",
                table: "attempts",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_ticket_id",
                table: "attempts",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_user_id",
                table: "attempts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attempts_user_id_started_at",
                table: "attempts",
                columns: new[] { "user_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_exam_id_position",
                table: "exam_questions",
                columns: new[] { "exam_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exam_questions_question_id",
                table: "exam_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_name",
                table: "groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_login_events_created_at",
                table: "login_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_login_events_user_id_created_at",
                table: "login_events",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_questions_topic_id",
                table: "questions",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_questions_question_id",
                table: "ticket_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_questions_ticket_id_position",
                table: "ticket_questions",
                columns: new[] { "ticket_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_topics_name",
                table: "topics",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_group_id",
                table: "users",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_login",
                table: "users",
                column: "normalized_login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_options");

            migrationBuilder.DropTable(
                name: "attempt_questions");

            migrationBuilder.DropTable(
                name: "exam_questions");

            migrationBuilder.DropTable(
                name: "login_events");

            migrationBuilder.DropTable(
                name: "ticket_questions");

            migrationBuilder.DropTable(
                name: "attempts");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "exams");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "groups");
        }
    }
}
