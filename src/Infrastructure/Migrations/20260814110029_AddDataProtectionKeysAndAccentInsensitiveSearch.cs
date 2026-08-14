using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionKeysAndAccentInsensitiveSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            // Accent- and case-insensitive substring search. `unaccent` is STABLE, not IMMUTABLE,
            // so it cannot be used in an expression index; f_unaccent is the standard IMMUTABLE
            // wrapper (the unaccent dictionary does not change in practice). pg_trgm supplies the
            // GIN operator class that keeps `lower(f_unaccent(col)) LIKE '%term%'` off a
            // sequential scan - add one index per searchable column, for example:
            //   CREATE INDEX IF NOT EXISTS ix_todolists_title_trgm
            //       ON "TodoLists" USING gin (lower(f_unaccent("Title")) gin_trgm_ops);
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS unaccent;
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION public.f_unaccent(text) RETURNS text
                LANGUAGE SQL IMMUTABLE PARALLEL SAFE AS $$
                    SELECT public.unaccent('public.unaccent'::regdictionary, $1)
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS public.f_unaccent(text);");
        }
    }
}
