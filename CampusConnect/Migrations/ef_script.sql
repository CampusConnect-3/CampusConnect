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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [category] (
        [categoryID] int NOT NULL IDENTITY,
        [categoryName] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_category] PRIMARY KEY ([categoryID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [requestStatus] (
        [statusID] int NOT NULL IDENTITY,
        [statusName] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_requestStatus] PRIMARY KEY ([statusID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [roles] (
        [roleID] int NOT NULL IDENTITY,
        [roleName] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_roles] PRIMARY KEY ([roleID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [users] (
        [userID] int NOT NULL IDENTITY,
        [fName] nvarchar(256) NOT NULL,
        [lName] nvarchar(256) NOT NULL,
        [username] nvarchar(256) NOT NULL,
        [password] nvarchar(256) NOT NULL,
        [email] nvarchar(256) NOT NULL,
        [department] nvarchar(256) NULL,
        [status] nvarchar(50) NULL,
        CONSTRAINT [PK_users] PRIMARY KEY ([userID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [request] (
        [requestID] int NOT NULL IDENTITY,
        [created_by] int NOT NULL,
        [assigned_to] int NULL,
        [title] nvarchar(256) NOT NULL,
        [description] nvarchar(1000) NOT NULL,
        [categoryID] int NOT NULL,
        [priority] nvarchar(10) NOT NULL,
        [statusID] int NULL,
        [createdAt] datetime2 NOT NULL,
        [closedAt] datetime2 NULL,
        [buildingName] nvarchar(256) NOT NULL,
        [roomNumber] nvarchar(256) NOT NULL,
        [phoneNumber] nvarchar(256) NULL,
        [email] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_request] PRIMARY KEY ([requestID]),
        CONSTRAINT [FK_request_category_categoryID] FOREIGN KEY ([categoryID]) REFERENCES [category] ([categoryID]) ON DELETE NO ACTION,
        CONSTRAINT [FK_request_requestStatus_statusID] FOREIGN KEY ([statusID]) REFERENCES [requestStatus] ([statusID]) ON DELETE SET NULL,
        CONSTRAINT [FK_request_users_assigned_to] FOREIGN KEY ([assigned_to]) REFERENCES [users] ([userID]) ON DELETE SET NULL,
        CONSTRAINT [FK_request_users_created_by] FOREIGN KEY ([created_by]) REFERENCES [users] ([userID]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [userRoles] (
        [roleID] int NOT NULL,
        [userID] int NOT NULL,
        CONSTRAINT [PK_userRoles] PRIMARY KEY ([roleID], [userID]),
        CONSTRAINT [FK_userRoles_roles_roleID] FOREIGN KEY ([roleID]) REFERENCES [roles] ([roleID]) ON DELETE CASCADE,
        CONSTRAINT [FK_userRoles_users_userID] FOREIGN KEY ([userID]) REFERENCES [users] ([userID]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [attachments] (
        [fileID] int NOT NULL IDENTITY,
        [requestID] int NOT NULL,
        [creatorID] int NOT NULL,
        [fileName] nvarchar(256) NULL,
        [contentType] nvarchar(256) NULL,
        [fileUrl] nvarchar(256) NOT NULL,
        [uploadedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_attachments] PRIMARY KEY ([fileID]),
        CONSTRAINT [FK_attachments_request_requestID] FOREIGN KEY ([requestID]) REFERENCES [request] ([requestID]) ON DELETE CASCADE,
        CONSTRAINT [FK_attachments_users_creatorID] FOREIGN KEY ([creatorID]) REFERENCES [users] ([userID]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE TABLE [requestComments] (
        [commentID] int NOT NULL IDENTITY,
        [requestID] int NOT NULL,
        [commentText] nvarchar(1000) NULL,
        [createdAt] datetime2 NOT NULL,
        [creatorID] int NOT NULL,
        CONSTRAINT [PK_requestComments] PRIMARY KEY ([commentID]),
        CONSTRAINT [FK_requestComments_request_requestID] FOREIGN KEY ([requestID]) REFERENCES [request] ([requestID]) ON DELETE CASCADE,
        CONSTRAINT [FK_requestComments_users_creatorID] FOREIGN KEY ([creatorID]) REFERENCES [users] ([userID]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_attachments_creatorID] ON [attachments] ([creatorID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_attachments_requestID] ON [attachments] ([requestID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_request_assigned_to] ON [request] ([assigned_to]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_request_categoryID] ON [request] ([categoryID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_request_created_by] ON [request] ([created_by]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_request_statusID] ON [request] ([statusID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_requestComments_creatorID] ON [requestComments] ([creatorID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_requestComments_requestID] ON [requestComments] ([requestID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    CREATE INDEX [IX_userRoles_userID] ON [userRoles] ([userID]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260209013000_TablesInit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260209013000_TablesInit', N'8.0.23');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'password');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [users] ALTER COLUMN [password] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[users]') AND [c].[name] = N'email');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [users] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [users] ALTER COLUMN [email] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    ALTER TABLE [users] ADD [IdentityUserId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    CREATE TABLE [Test_Brittany_L] (
        [testID] int NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [Score] int NOT NULL,
        [Email] nvarchar(max) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Test_Brittany_L] PRIMARY KEY ([testID])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_users_IdentityUserId] ON [users] ([IdentityUserId]) WHERE [IdentityUserId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    CREATE UNIQUE INDEX [IX_users_username] ON [users] ([username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260226004514_TestBrittanyLTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260226004514_TestBrittanyLTable', N'8.0.23');
END;
GO

COMMIT;
GO

