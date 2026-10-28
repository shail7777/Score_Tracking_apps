IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE TABLE [Players] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [Position] nvarchar(5) NOT NULL,
        [HeightInches] int NULL,
        [WeightLbs] int NULL,
        [BirthDate] date NULL,
        CONSTRAINT [PK_Players] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE TABLE [Teams] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [Abbreviation] nvarchar(5) NOT NULL,
        [Conference] nvarchar(10) NOT NULL,
        [Division] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Teams] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE TABLE [Games] (
        [Id] int NOT NULL IDENTITY,
        [Season] nvarchar(7) NOT NULL,
        [GameDate] datetime2 NOT NULL,
        [Arena] nvarchar(100) NULL,
        [Status] nvarchar(20) NOT NULL,
        [HomeTeamId] int NOT NULL,
        [AwayTeamId] int NOT NULL,
        [HomeScore] int NULL,
        [AwayScore] int NULL,
        CONSTRAINT [PK_Games] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Games_Teams_AwayTeamId] FOREIGN KEY ([AwayTeamId]) REFERENCES [Teams] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Games_Teams_HomeTeamId] FOREIGN KEY ([HomeTeamId]) REFERENCES [Teams] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE TABLE [TeamPlayers] (
        [Id] int NOT NULL IDENTITY,
        [TeamId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [JerseyNumber] int NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        CONSTRAINT [PK_TeamPlayers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TeamPlayers_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TeamPlayers_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Games_AwayTeamId] ON [Games] ([AwayTeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Games_GameDate] ON [Games] ([GameDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Games_HomeTeamId] ON [Games] ([HomeTeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TeamPlayers_PlayerId] ON [TeamPlayers] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeamPlayers_TeamId_PlayerId_StartDate] ON [TeamPlayers] ([TeamId], [PlayerId], [StartDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Teams_Abbreviation] ON [Teams] ([Abbreviation]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027203952_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027203952_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027210232_AddRosterIndex'
)
BEGIN
    CREATE INDEX [IX_TeamPlayers_TeamId_EndDate] ON [TeamPlayers] ([TeamId], [EndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027210232_AddRosterIndex'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027210232_AddRosterIndex', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027210851_RemoveJerseyNumberAndPosition'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TeamPlayers]') AND [c].[name] = N'JerseyNumber');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TeamPlayers] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [TeamPlayers] DROP COLUMN [JerseyNumber];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027210851_RemoveJerseyNumberAndPosition'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Players]') AND [c].[name] = N'Position');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Players] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Players] DROP COLUMN [Position];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027210851_RemoveJerseyNumberAndPosition'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027210851_RemoveJerseyNumberAndPosition', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211407_RemoveTeamAbbreviation'
)
BEGIN
    DROP INDEX [IX_Teams_Abbreviation] ON [Teams];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211407_RemoveTeamAbbreviation'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Teams]') AND [c].[name] = N'Abbreviation');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Teams] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Teams] DROP COLUMN [Abbreviation];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211407_RemoveTeamAbbreviation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027211407_RemoveTeamAbbreviation', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211849_AddGameTeamDateIndexes'
)
BEGIN
    DROP INDEX [IX_Games_AwayTeamId] ON [Games];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211849_AddGameTeamDateIndexes'
)
BEGIN
    DROP INDEX [IX_Games_HomeTeamId] ON [Games];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211849_AddGameTeamDateIndexes'
)
BEGIN
    CREATE INDEX [IX_Games_AwayTeamId_GameDate] ON [Games] ([AwayTeamId], [GameDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211849_AddGameTeamDateIndexes'
)
BEGIN
    CREATE INDEX [IX_Games_HomeTeamId_GameDate] ON [Games] ([HomeTeamId], [GameDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211849_AddGameTeamDateIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027211849_AddGameTeamDateIndexes', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211951_AddScoringEvents'
)
BEGIN
    CREATE TABLE [ScoringEvents] (
        [Id] int NOT NULL IDENTITY,
        [GameId] int NOT NULL,
        [TeamId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [Points] tinyint NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_ScoringEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ScoringEvents_Points] CHECK ([Points] IN (2, 3)),
        CONSTRAINT [FK_ScoringEvents_Games_GameId] FOREIGN KEY ([GameId]) REFERENCES [Games] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ScoringEvents_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScoringEvents_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211951_AddScoringEvents'
)
BEGIN
    CREATE INDEX [IX_ScoringEvents_GameId_CreatedAt] ON [ScoringEvents] ([GameId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211951_AddScoringEvents'
)
BEGIN
    CREATE INDEX [IX_ScoringEvents_PlayerId] ON [ScoringEvents] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211951_AddScoringEvents'
)
BEGIN
    CREATE INDEX [IX_ScoringEvents_TeamId] ON [ScoringEvents] ([TeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027211951_AddScoringEvents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027211951_AddScoringEvents', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027212746_AddGameRowVersion'
)
BEGIN
    ALTER TABLE [Games] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027212746_AddGameRowVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027212746_AddGameRowVersion', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    ALTER TABLE [ScoringEvents] ADD [ClockSecondsRemaining] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    ALTER TABLE [ScoringEvents] ADD [Quarter] tinyint NOT NULL DEFAULT CAST(1 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    ALTER TABLE [Games] ADD [ClockSecondsRemaining] int NOT NULL DEFAULT 720;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    ALTER TABLE [Games] ADD [Quarter] tinyint NOT NULL DEFAULT CAST(1 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    EXEC(N'ALTER TABLE [ScoringEvents] ADD CONSTRAINT [CK_ScoringEvents_Clock] CHECK ([ClockSecondsRemaining] IS NULL OR [ClockSecondsRemaining] BETWEEN 0 AND 720)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    EXEC(N'ALTER TABLE [ScoringEvents] ADD CONSTRAINT [CK_ScoringEvents_Quarter] CHECK ([Quarter] BETWEEN 1 AND 10)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    EXEC(N'ALTER TABLE [Games] ADD CONSTRAINT [CK_Games_Clock] CHECK ([ClockSecondsRemaining] BETWEEN 0 AND 720)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    EXEC(N'ALTER TABLE [Games] ADD CONSTRAINT [CK_Games_Quarter] CHECK ([Quarter] BETWEEN 1 AND 10)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027213723_AddGameClockAndEventTiming'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027213723_AddGameClockAndEventTiming', N'10.0.14');
END;

COMMIT;
GO

