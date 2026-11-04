using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameIdSequencesToCodeSequences : Migration
    {
        // A rename, not the drop and create the scaffolder emitted: the table holds the HiLo
        // high-water marks, and losing them would hand out codes that already exist.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_IdSequences", table: "IdSequences");
            migrationBuilder.RenameTable(name: "IdSequences", newName: "CodeSequences");
            migrationBuilder.AddPrimaryKey(name: "PK_CodeSequences", table: "CodeSequences", column: "Radical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_CodeSequences", table: "CodeSequences");
            migrationBuilder.RenameTable(name: "CodeSequences", newName: "IdSequences");
            migrationBuilder.AddPrimaryKey(name: "PK_IdSequences", table: "IdSequences", column: "Radical");
        }
    }
}
