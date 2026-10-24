using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LunaChinese.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    CanonicalKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.CanonicalKey);
                });

            migrationBuilder.CreateTable(
                name: "Aliases",
                columns: table => new
                {
                    Alias = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CanonicalKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aliases", x => x.Alias);
                    table.ForeignKey(
                        name: "FK_Aliases_Analyses_CanonicalKey",
                        column: x => x.CanonicalKey,
                        principalTable: "Analyses",
                        principalColumn: "CanonicalKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_CanonicalKey",
                table: "Aliases",
                column: "CanonicalKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Aliases");

            migrationBuilder.DropTable(
                name: "Analyses");
        }
    }
}
