using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HackathonGame.SessionService.Migrations
{
    /// <inheritdoc />
    public partial class AddPausedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "paused_at",
                table: "sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "paused_at",
                table: "sessions");
        }
    }
}
