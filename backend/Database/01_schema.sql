-- 01_schema.sql
-- Генерирано от миграцията InitialCreate с командата:
--     Script-Migration 0 InitialCreate
-- Таблиците НЕ се пишат на ръка: това е SQL кодът, който EF Core създава от
-- класовете в Model/Entities и AppDbContext. Не е нужно да се пуска —
-- базата се създава от миграциите (Update-Database или при първото стартиране).

﻿IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [DeviceTypes] (
    [DeviceTypeId] int NOT NULL IDENTITY,
    [Name] nvarchar(60) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_DeviceTypes] PRIMARY KEY ([DeviceTypeId])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Address] nvarchar(200) NULL,
    [City] nvarchar(60) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [IsBlocked] bit NOT NULL DEFAULT CAST(0 AS bit),
    [AvgRating] decimal(3,2) NULL,
    [ReviewsCount] int NOT NULL DEFAULT 0,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_Users_AvgRating] CHECK ([AvgRating] IS NULL OR [AvgRating] BETWEEN 1 AND 5),
    CONSTRAINT [CK_Users_ReviewsCount] CHECK ([ReviewsCount] >= 0),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (N'Admin', N'Client', N'Technician'))
);

CREATE TABLE [SupportRequests] (
    [SupportRequestId] int NOT NULL IDENTITY,
    [ClientId] int NOT NULL,
    [DeviceTypeId] int NOT NULL,
    [Title] nvarchar(120) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [DeviceModel] nvarchar(100) NULL,
    [City] nvarchar(60) NOT NULL,
    [PreferredDate] date NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Open',
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SupportRequests] PRIMARY KEY ([SupportRequestId]),
    CONSTRAINT [CK_SupportRequests_Status] CHECK ([Status] IN (N'Open', N'Assigned', N'Completed', N'Cancelled')),
    CONSTRAINT [FK_SupportRequests_DeviceTypes_DeviceTypeId] FOREIGN KEY ([DeviceTypeId]) REFERENCES [DeviceTypes] ([DeviceTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SupportRequests_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [TechnicianDeviceTypes] (
    [TechnicianId] int NOT NULL,
    [DeviceTypeId] int NOT NULL,
    CONSTRAINT [PK_TechnicianDeviceTypes] PRIMARY KEY ([TechnicianId], [DeviceTypeId]),
    CONSTRAINT [FK_TechnicianDeviceTypes_DeviceTypes_DeviceTypeId] FOREIGN KEY ([DeviceTypeId]) REFERENCES [DeviceTypes] ([DeviceTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TechnicianDeviceTypes_Users_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Offers] (
    [OfferId] int NOT NULL IDENTITY,
    [SupportRequestId] int NOT NULL,
    [TechnicianId] int NOT NULL,
    [Price] decimal(10,2) NOT NULL,
    [Message] nvarchar(1000) NULL,
    [ProposedDate] date NULL,
    [Status] nvarchar(20) NOT NULL DEFAULT N'Pending',
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Offers] PRIMARY KEY ([OfferId]),
    CONSTRAINT [CK_Offers_Price] CHECK ([Price] > 0),
    CONSTRAINT [CK_Offers_Status] CHECK ([Status] IN (N'Pending', N'Accepted', N'Rejected', N'Withdrawn')),
    CONSTRAINT [FK_Offers_SupportRequests_SupportRequestId] FOREIGN KEY ([SupportRequestId]) REFERENCES [SupportRequests] ([SupportRequestId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_Users_TechnicianId] FOREIGN KEY ([TechnicianId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Reviews] (
    [ReviewId] int NOT NULL IDENTITY,
    [SupportRequestId] int NOT NULL,
    [Rating] tinyint NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
    CONSTRAINT [FK_Reviews_SupportRequests_SupportRequestId] FOREIGN KEY ([SupportRequestId]) REFERENCES [SupportRequests] ([SupportRequestId]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DeviceTypeId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[DeviceTypes]'))
    SET IDENTITY_INSERT [DeviceTypes] ON;
INSERT INTO [DeviceTypes] ([DeviceTypeId], [IsActive], [Name])
VALUES (1, CAST(1 AS bit), N'Лаптоп'),
(2, CAST(1 AS bit), N'Настолен компютър'),
(3, CAST(1 AS bit), N'Смартфон'),
(4, CAST(1 AS bit), N'Таблет'),
(5, CAST(1 AS bit), N'Принтер'),
(6, CAST(1 AS bit), N'Рутер и мрежа');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'DeviceTypeId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[DeviceTypes]'))
    SET IDENTITY_INSERT [DeviceTypes] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'Address', N'AvgRating', N'City', N'CreatedAt', N'Email', N'FullName', N'PasswordHash', N'Phone', N'Role') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([UserId], [Address], [AvgRating], [City], [CreatedAt], [Email], [FullName], [PasswordHash], [Phone], [Role])
VALUES (1, NULL, NULL, N'Враца', '2026-01-01T00:00:00.0000000Z', N'admin@itspec.local', N'Администратор', N'AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg97z2IZyhqC3YaJEn8frGtw7fMb52Ky5VJvyBDCaUXU9w==', NULL, N'Admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'UserId', N'Address', N'AvgRating', N'City', N'CreatedAt', N'Email', N'FullName', N'PasswordHash', N'Phone', N'Role') AND [object_id] = OBJECT_ID(N'[Users]'))
    SET IDENTITY_INSERT [Users] OFF;

CREATE UNIQUE INDEX [IX_DeviceTypes_Name] ON [DeviceTypes] ([Name]);

CREATE UNIQUE INDEX [IX_Offers_SupportRequestId_TechnicianId] ON [Offers] ([SupportRequestId], [TechnicianId]);

CREATE INDEX [IX_Offers_TechnicianId] ON [Offers] ([TechnicianId]);

CREATE UNIQUE INDEX [UX_Offers_OneAccepted] ON [Offers] ([SupportRequestId]) WHERE [Status] = N'Accepted';

CREATE UNIQUE INDEX [IX_Reviews_SupportRequestId] ON [Reviews] ([SupportRequestId]);

CREATE INDEX [IX_SupportRequests_ClientId] ON [SupportRequests] ([ClientId]);

CREATE INDEX [IX_SupportRequests_DeviceTypeId_Status] ON [SupportRequests] ([DeviceTypeId], [Status]);

CREATE INDEX [IX_TechnicianDeviceTypes_DeviceTypeId] ON [TechnicianDeviceTypes] ([DeviceTypeId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261021051350_InitialCreate', N'10.0.12');

COMMIT;
GO

