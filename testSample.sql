IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

GO

CREATE TABLE [Runs] (
    [Id] int NOT NULL IDENTITY,
    [Start] datetimeoffset NOT NULL,
    [End] datetimeoffset NOT NULL,
    [Status] int NOT NULL,
    [Name] nvarchar(max) NULL,
    [Guid] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Runs] PRIMARY KEY ([Id])
);

GO

CREATE TABLE [WorkItems] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [State] int NOT NULL,
    [Timestamp] rowversion NULL,
    CONSTRAINT [PK_WorkItems] PRIMARY KEY ([Id])
);

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20200726130732_Init', N'3.1.6');

GO

