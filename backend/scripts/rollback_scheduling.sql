IF OBJECT_ID(N'[scheduling].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'scheduling') IS NULL EXEC(N'CREATE SCHEMA [scheduling];');
    CREATE TABLE [scheduling].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [scheduling].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419090546_V001_InitialSchema'
)
BEGIN
    INSERT INTO [scheduling].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419090546_V001_InitialSchema', N'8.0.11');
END;
GO

COMMIT;
GO

