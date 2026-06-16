BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419114528_V005_SeedInsuranceRecords'
)
BEGIN
    CREATE TABLE [identity].[InsurancePlans] (
        [Id] uniqueidentifier NOT NULL,
        [InsuranceName] nvarchar(200) NOT NULL,
        [ValidMemberIdPattern] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        CONSTRAINT [PK_InsurancePlans] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419114528_V005_SeedInsuranceRecords'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'InsuranceName', N'IsActive', N'IsDeleted', N'UpdatedAt', N'ValidMemberIdPattern') AND [object_id] = OBJECT_ID(N'[identity].[InsurancePlans]'))
        SET IDENTITY_INSERT [identity].[InsurancePlans] ON;
    EXEC(N'INSERT INTO [identity].[InsurancePlans] ([Id], [CreatedAt], [DeletedAt], [InsuranceName], [IsActive], [IsDeleted], [UpdatedAt], [ValidMemberIdPattern])
    VALUES (''a1b2c3d4-0001-0000-0000-000000000001'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Blue Cross Blue Shield'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^[A-Z]{3}\d{9}$''),
    (''a1b2c3d4-0002-0000-0000-000000000002'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Aetna'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^\d{8,12}$''),
    (''a1b2c3d4-0003-0000-0000-000000000003'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''UnitedHealthcare'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^U\d{9}$''),
    (''a1b2c3d4-0004-0000-0000-000000000004'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Cigna'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^\d{10}$''),
    (''a1b2c3d4-0005-0000-0000-000000000005'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Humana'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^H\d{8}$''),
    (''a1b2c3d4-0006-0000-0000-000000000006'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Kaiser Permanente'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^\d{8,10}$''),
    (''a1b2c3d4-0007-0000-0000-000000000007'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Anthem'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^[A-Z]{2}\d{9}$''),
    (''a1b2c3d4-0008-0000-0000-000000000008'', ''2026-04-19T00:00:00.0000000Z'', NULL, N''Molina Healthcare'', CAST(1 AS bit), CAST(0 AS bit), ''2026-04-19T00:00:00.0000000Z'', N''^\d{9,12}$'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DeletedAt', N'InsuranceName', N'IsActive', N'IsDeleted', N'UpdatedAt', N'ValidMemberIdPattern') AND [object_id] = OBJECT_ID(N'[identity].[InsurancePlans]'))
        SET IDENTITY_INSERT [identity].[InsurancePlans] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419114528_V005_SeedInsuranceRecords'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InsurancePlan_InsuranceName] ON [identity].[InsurancePlans] ([InsuranceName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [identity].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419114528_V005_SeedInsuranceRecords'
)
BEGIN
    INSERT INTO [identity].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419114528_V005_SeedInsuranceRecords', N'8.0.11');
END;
GO

COMMIT;
GO

