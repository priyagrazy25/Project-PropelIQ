IF OBJECT_ID(N'[clinical].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'clinical') IS NULL EXEC(N'CREATE SCHEMA [clinical];');
    CREATE TABLE [clinical].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [clinical].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419090613_V001_InitialSchema'
)
BEGIN
    INSERT INTO [clinical].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419090613_V001_InitialSchema', N'8.0.11');
END;
GO

COMMIT;
GO

