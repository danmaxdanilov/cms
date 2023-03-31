using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.API.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cms_schema");

            migrationBuilder.CreateTable(
                name: "entry",
                schema: "cms_schema",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    PlistFileName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "command",
                schema: "cms_schema",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    entryId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command", x => x.Id);
                    table.ForeignKey(
                        name: "FK_command_entry_entryId",
                        column: x => x.entryId,
                        principalSchema: "cms_schema",
                        principalTable: "entry",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_command_entryId",
                schema: "cms_schema",
                table: "command",
                column: "entryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command",
                schema: "cms_schema");

            migrationBuilder.DropTable(
                name: "entry",
                schema: "cms_schema");
        }
    }
}
