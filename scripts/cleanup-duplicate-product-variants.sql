/*
	Cleanup duplicate ProductVariants by ProductId + normalized ProductValueId combination.
	- Keeps the earliest variant (CreatedAt, then ProductVariantId)
	- Remaps OrderDetails to the kept variant
	- Deletes duplicate variants

	Run this script before creating unique index IX_ProductVariants_ProductId_CombinationKey
	if your environment already contains duplicate rows.
*/

BEGIN TRY
	BEGIN TRANSACTION;

	IF OBJECT_ID('tempdb..#VariantKeyMap') IS NOT NULL
		DROP TABLE #VariantKeyMap;

	CREATE TABLE #VariantKeyMap
	(
		ProductVariantId uniqueidentifier NOT NULL PRIMARY KEY,
		ProductId uniqueidentifier NOT NULL,
		CombinationKey varchar(900) NOT NULL
	);

	INSERT INTO #VariantKeyMap (ProductVariantId, ProductId, CombinationKey)
	SELECT
		pv.ProductVariantId,
		pv.ProductId,
		COALESCE(
			STUFF
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
			),
			''
		) AS CombinationKey
	FROM ProductVariants pv;

	;WITH RankedVariants AS
	(
		SELECT
			pv.ProductVariantId,
			map.ProductId,
			map.CombinationKey,
			ROW_NUMBER() OVER
			(
				PARTITION BY map.ProductId, map.CombinationKey
				ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
			) AS RowNum,
			FIRST_VALUE(pv.ProductVariantId) OVER
			(
				PARTITION BY map.ProductId, map.CombinationKey
				ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
			) AS KeepVariantId
		FROM ProductVariants pv
		INNER JOIN #VariantKeyMap map ON pv.ProductVariantId = map.ProductVariantId
	)
	UPDATE od
	SET od.ProductVariantId = rv.KeepVariantId
	FROM OrderDetails od
	INNER JOIN RankedVariants rv ON od.ProductVariantId = rv.ProductVariantId
	WHERE rv.RowNum > 1 AND rv.ProductVariantId <> rv.KeepVariantId;

	;WITH RankedVariants AS
	(
		SELECT
			pv.ProductVariantId,
			map.ProductId,
			map.CombinationKey,
			ROW_NUMBER() OVER
			(
				PARTITION BY map.ProductId, map.CombinationKey
				ORDER BY ISNULL(pv.CreatedAt, '19000101'), pv.ProductVariantId
			) AS RowNum
		FROM ProductVariants pv
		INNER JOIN #VariantKeyMap map ON pv.ProductVariantId = map.ProductVariantId
	)
	DELETE pv
	FROM ProductVariants pv
	INNER JOIN RankedVariants rv ON pv.ProductVariantId = rv.ProductVariantId
	WHERE rv.RowNum > 1;

	COMMIT TRANSACTION;
END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0
		ROLLBACK TRANSACTION;

	THROW;
END CATCH;
