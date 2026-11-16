using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Atlas.Infrastructure.Persistence.Migrations
{
    // Round 28 (CLAUDE.md "Вход и роли"): the Crew role became Manager. Data only, the model does not change.
    // The roles are created by IdentitySeeder (from Roles.All), not by a migration, so:
    // - an existing database has a Crew row: it is renamed, its users become Managers;
    // - a new database has no roles yet when the migrations run: nothing happens, the seed creates Manager;
    // - the seed may already have created Manager while Crew still has users (production applies migrations
    //   separately from the start): the users are moved to Manager and Crew is deleted.
    /// <inheritdoc />
    public partial class RenameCrewToManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(RenameRole(from: "Crew", to: "Manager"));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(RenameRole(from: "Manager", to: "Crew"));

        private static string RenameRole(string from, string to)
        {
            var fromNormalized = from.ToUpperInvariant();
            var toNormalized = to.ToUpperInvariant();

            return $"""
                -- Both roles exist: move the users of '{from}' to '{to}' (one role per user, but never twice).
                UPDATE "AspNetUserRoles" AS ur
                SET "RoleId" = target."Id"
                FROM "AspNetRoles" AS source, "AspNetRoles" AS target
                WHERE source."NormalizedName" = '{fromNormalized}'
                  AND target."NormalizedName" = '{toNormalized}'
                  AND ur."RoleId" = source."Id"
                  AND NOT EXISTS (
                      SELECT 1 FROM "AspNetUserRoles" AS other
                      WHERE other."UserId" = ur."UserId" AND other."RoleId" = target."Id");

                -- ... and drop '{from}' (any links left go with it: the FKs cascade).
                DELETE FROM "AspNetRoles"
                WHERE "NormalizedName" = '{fromNormalized}'
                  AND EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = '{toNormalized}');

                -- Only '{from}' exists: rename it, so its users keep their role row.
                UPDATE "AspNetRoles"
                SET "Name" = '{to}', "NormalizedName" = '{toNormalized}', "ConcurrencyStamp" = gen_random_uuid()::text
                WHERE "NormalizedName" = '{fromNormalized}';
                """;
        }
    }
}
