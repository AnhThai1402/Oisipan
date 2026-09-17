BEGIN TRANSACTION;
ALTER TABLE [ProductVariants] ADD [DesignData] nvarchar(max) NULL;

ALTER TABLE [ProductVariants] ADD [IsDesignerEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260916085036_ProductVariantDesignerUpdate', N'10.0.8');

COMMIT;
GO

