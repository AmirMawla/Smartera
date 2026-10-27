using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smartera.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OtpEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HashedOtp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtpEntries_Email_Purpose",
                table: "OtpEntries",
                columns: new[] { "Email", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpEntries_Expiry",
                table: "OtpEntries",
                column: "Expiry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpEntries");
        }
    }
}
