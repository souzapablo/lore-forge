using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace LoreForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgressSnapshot = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    FileRef = table.Column<string>(type: "text", nullable: true),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Works",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Genres = table.Column<string[]>(type: "text[]", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Progress = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Notes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Works", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorldNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldNotes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "Works");

            migrationBuilder.DropTable(
                name: "WorldNotes");
        }
    }
}
