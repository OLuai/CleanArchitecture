using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Security = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UserName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProtectedPassword = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FromAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FromName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSettings");
        }
    }
}
