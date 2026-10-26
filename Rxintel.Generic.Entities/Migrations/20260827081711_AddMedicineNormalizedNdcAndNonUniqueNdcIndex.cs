using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rxintel.Generic.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineNormalizedNdcAndNonUniqueNdcIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: live DBs may already have these objects from the product import fix.
            migrationBuilder.Sql("""
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Medicine_NormalizedNdc'
                      AND object_id = OBJECT_ID(N'dbo.Medicine')
                )
                    DROP INDEX [IX_Medicine_NormalizedNdc] ON [dbo].[Medicine];

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Medicine_NDC'
                      AND object_id = OBJECT_ID(N'dbo.Medicine')
                )
                    DROP INDEX [IX_Medicine_NDC] ON [dbo].[Medicine];

                IF COL_LENGTH(N'dbo.Medicine', N'NormalizedNdc') IS NOT NULL
                    ALTER TABLE [dbo].[Medicine] DROP COLUMN [NormalizedNdc];

                ALTER TABLE [dbo].[Medicine]
                    ADD [NormalizedNdc] AS (CAST(REPLACE([NDC], N'-', N'') AS nvarchar(100))) PERSISTED;

                CREATE NONCLUSTERED INDEX [IX_Medicine_NDC]
                    ON [dbo].[Medicine] ([NDC]);

                CREATE NONCLUSTERED INDEX [IX_Medicine_NormalizedNdc]
                    ON [dbo].[Medicine] ([NormalizedNdc]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Medicine_NormalizedNdc'
                      AND object_id = OBJECT_ID(N'dbo.Medicine')
                )
                    DROP INDEX [IX_Medicine_NormalizedNdc] ON [dbo].[Medicine];

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Medicine_NDC'
                      AND object_id = OBJECT_ID(N'dbo.Medicine')
                )
                    DROP INDEX [IX_Medicine_NDC] ON [dbo].[Medicine];

                IF COL_LENGTH(N'dbo.Medicine', N'NormalizedNdc') IS NOT NULL
                    ALTER TABLE [dbo].[Medicine] DROP COLUMN [NormalizedNdc];

                CREATE UNIQUE NONCLUSTERED INDEX [IX_Medicine_NDC]
                    ON [dbo].[Medicine] ([NDC]);
                """);
        }
    }
}
