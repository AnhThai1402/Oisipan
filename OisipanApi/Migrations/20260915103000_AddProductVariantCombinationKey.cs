using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantCombinationKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CombinationKey",
                table: "ProductVariants",
                type: "varchar(900)",
                maxLength: 900,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE pv
SET pv.CombinationKey = COALESCE(keys.CombinationKey, '')
FROM ProductVariants pv
OUTER APPLY
(
    SELECT STUFF
    (
        (
            SELECT '|' + LOWER(REPLACE(CONVERT(varchar(36), pvv2.ProductValueId), '-', ''))
            FROM ProductVariantValues pvv2
            WHERE pvv2.ProductVariantId = pv.ProductVariantId
            GROUP BY pvv2.ProductValueId
            ORDER BY pvv2.ProductValueId
            FOR XML PATH(''), TYPE
        ).value('.', 'nvarchar(max)'),
        1,
        1,
        ''
    ) AS CombinationKey
) keys;
");

            migrationBuilder.Sql(@"
;WITH RankedVariants AS
(
    SELECT
        pv.ProductVariantId,
        pv.ProductId,
        pv.CombinationKey,
        ROW_NUMBER() OVER
        (
            PARTITION BY pv.ProductId, pv.CombinationKey
            ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
        ) AS RowNum,
        FIRST_VALUE(pv.ProductVariantId) OVER
        (
            PARTITION BY pv.ProductId, pv.CombinationKey
            ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
        ) AS KeepVariantId
    FROM ProductVariants pv
    WHERE pv.CombinationKey IS NOT NULL
)
UPDATE od
SET od.ProductVariantId = rv.KeepVariantId
FROM OrderDetails od
INNER JOIN RankedVariants rv ON od.ProductVariantId = rv.ProductVariantId
WHERE rv.RowNum > 1 AND rv.ProductVariantId <> rv.KeepVariantId;
");

            migrationBuilder.Sql(@"
;WITH RankedVariants AS
(
    SELECT
        pv.ProductVariantId,
        pv.ProductId,
        pv.CombinationKey,
        ROW_NUMBER() OVER
        (
            PARTITION BY pv.ProductId, pv.CombinationKey
            ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
        ) AS RowNum
    FROM ProductVariants pv
    WHERE pv.CombinationKey IS NOT NULL
)
DELETE pv
FROM ProductVariants pv
INNER JOIN RankedVariants rv ON pv.ProductVariantId = rv.ProductVariantId
WHERE rv.RowNum > 1;
");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                table: "ProductVariants",
                columns: new[] { "ProductId", "CombinationKey" },
                unique: true,
                filter: "[CombinationKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductId_CombinationKey",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CombinationKey",
                table: "ProductVariants");
        }
    }
}
