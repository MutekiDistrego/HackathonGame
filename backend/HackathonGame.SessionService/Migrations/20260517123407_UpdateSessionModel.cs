using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HackathonGame.SessionService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "round_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<long>(type: "bigint", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    team_count = table.Column<int>(type: "integer", nullable: false),
                    track = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_duration_minutes = table.Column<decimal>(type: "numeric", nullable: true),
                    planned_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_round_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_round_history_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_round_history_ml",
                table: "round_history",
                columns: new[] { "round_number", "team_count", "track" },
                filter: "actual_duration_minutes IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_round_history_session",
                table: "round_history",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "round_history");
        }
    }
}
