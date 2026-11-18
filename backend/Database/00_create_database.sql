-- 00_create_database.sql
-- Създава базата ITspec точно както Update-Database:
--   1) миграцията InitialCreate (таблиците от C# класовете; SQL кодът е генериран от EF, не е писан на ръка);
--   2) миграцията SqlObjects (табличен тип, процедури, изгледи — от Model/Data/Sql/SqlObjects.cs).
-- Нужно е само ако базата се създава от SSMS. Иначе приложението я създава само при първото стартиране.
-- Пуска се веднъж в празен сървър (F5 в SSMS).

IF DB_ID(N'ITspec') IS NULL
    CREATE DATABASE ITspec;
GO
USE ITspec;
GO

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

IF TYPE_ID(N'dbo.IdListType') IS NULL
    EXEC(N'CREATE TYPE dbo.IdListType AS TABLE (Id INT NOT NULL PRIMARY KEY);');
GO

-- Приемане на оферта. Всичко или нищо:
--   офертата става Accepted, останалите чакащи стават Rejected, заявката става Assigned.
-- Собствено правило (тема 8): техник не може да има повече от 3 поети (Assigned) заявки едновременно.
CREATE OR ALTER PROCEDURE dbo.usp_AcceptOffer
    @OfferId  INT,
    @ClientId INT          -- идва от JWT токена, никога от тялото на заявката
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RequestId     INT,
            @RequestStatus NVARCHAR(20),
            @OwnerId       INT,
            @OfferStatus   NVARCHAR(20),
            @TechnicianId  INT,
            @ActiveJobs    INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @RequestId = SupportRequestId
        FROM dbo.Offers
        WHERE OfferId = @OfferId;

        IF @RequestId IS NULL
            THROW 50001, N'Офертата не съществува.', 1;

        -- 1. Заключваме първо заявката, после офертата (винаги в този ред, за да няма deadlock).
        SELECT @RequestStatus = Status, @OwnerId = ClientId
        FROM dbo.SupportRequests WITH (UPDLOCK, HOLDLOCK)
        WHERE SupportRequestId = @RequestId;

        SELECT @OfferStatus = Status, @TechnicianId = TechnicianId
        FROM dbo.Offers WITH (UPDLOCK, HOLDLOCK)
        WHERE OfferId = @OfferId;

        -- 2. Проверки. При грешка: THROW -> CATCH -> ROLLBACK, нищо не се променя.
        IF @OwnerId <> @ClientId
            THROW 50002, N'Можете да приемате оферти само по свои заявки.', 1;

        IF @RequestStatus <> N'Open'
            THROW 50003, N'Заявката вече не е отворена.', 1;

        IF @OfferStatus <> N'Pending'
            THROW 50004, N'Офертата вече не е активна.', 1;

        -- 3. Собствено правило: заключваме и техника, за да не го приемат двама клиенти едновременно.
        SELECT @TechnicianId = UserId
        FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
        WHERE UserId = @TechnicianId;

        SELECT @ActiveJobs = COUNT(*)
        FROM dbo.Offers o
        JOIN dbo.SupportRequests r ON r.SupportRequestId = o.SupportRequestId
        WHERE o.TechnicianId = @TechnicianId
          AND o.Status = N'Accepted'
          AND r.Status = N'Assigned';

        IF @ActiveJobs >= 3
            THROW 50005, N'Техникът вече има 3 поети заявки и не може да поеме нова. Изберете друга оферта.', 1;

        -- 4. Промени: всички или нито една.
        UPDATE dbo.Offers
        SET Status = N'Accepted'
        WHERE OfferId = @OfferId;

        UPDATE dbo.Offers
        SET Status = N'Rejected'
        WHERE SupportRequestId = @RequestId
          AND OfferId <> @OfferId
          AND Status = N'Pending';

        UPDATE dbo.SupportRequests
        SET Status = N'Assigned'
        WHERE SupportRequestId = @RequestId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Отзив за завършена заявка. Всичко или нищо:
--   записва отзива и преизчислява AvgRating и ReviewsCount на техника.
CREATE OR ALTER PROCEDURE dbo.usp_AddReview
    @SupportRequestId INT,
    @ClientId         INT,              -- идва от JWT токена
    @Rating           TINYINT,
    @Comment          NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OwnerId       INT,
            @RequestStatus NVARCHAR(20),
            @TechnicianId  INT,
            @NewAvg        DECIMAL(3, 2),
            @NewCount      INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Заключваме заявката.
        SELECT @OwnerId = ClientId, @RequestStatus = Status
        FROM dbo.SupportRequests WITH (UPDLOCK, HOLDLOCK)
        WHERE SupportRequestId = @SupportRequestId;

        -- 2. Проверки.
        IF @OwnerId IS NULL
            THROW 50006, N'Заявката не съществува.', 1;

        IF @Rating NOT BETWEEN 1 AND 5
            THROW 50007, N'Оценката трябва да е от 1 до 5.', 1;

        IF @OwnerId <> @ClientId
            THROW 50008, N'Можете да оставите отзив само за своя заявка.', 1;

        IF @RequestStatus <> N'Completed'
            THROW 50009, N'Отзив се оставя само за завършена заявка.', 1;

        IF EXISTS (SELECT 1 FROM dbo.Reviews WHERE SupportRequestId = @SupportRequestId)
            THROW 50010, N'Тази заявка вече има отзив.', 1;

        SELECT @TechnicianId = TechnicianId
        FROM dbo.Offers
        WHERE SupportRequestId = @SupportRequestId AND Status = N'Accepted';

        -- 3. Промени: отзивът и рейтингът на техника заедно.
        INSERT INTO dbo.Reviews (SupportRequestId, Rating, Comment)
        VALUES (@SupportRequestId, @Rating, NULLIF(LTRIM(RTRIM(@Comment)), N''));

        SELECT @NewAvg   = CAST(AVG(CAST(rv.Rating AS DECIMAL(5, 2))) AS DECIMAL(3, 2)),
               @NewCount = COUNT(*)
        FROM dbo.Reviews rv
        JOIN dbo.Offers o ON o.SupportRequestId = rv.SupportRequestId AND o.Status = N'Accepted'
        WHERE o.TechnicianId = @TechnicianId;

        UPDATE dbo.Users
        SET AvgRating = @NewAvg,
            ReviewsCount = @NewCount
        WHERE UserId = @TechnicianId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Смяна на видовете устройства на техник наведнъж, през табличния тип.
-- Всичко или нищо: при невалиден списък старите видове остават.
CREATE OR ALTER PROCEDURE dbo.usp_SetTechnicianDeviceTypes
    @TechnicianId  INT,                        -- идва от JWT токена
    @DeviceTypeIds dbo.IdListType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                       WHERE UserId = @TechnicianId AND Role = N'Technician')
            THROW 50011, N'Потребителят не е техник.', 1;

        IF NOT EXISTS (SELECT 1 FROM @DeviceTypeIds)
            THROW 50012, N'Изберете поне един вид устройство.', 1;

        IF EXISTS (SELECT 1
                   FROM @DeviceTypeIds i
                   LEFT JOIN dbo.DeviceTypes d ON d.DeviceTypeId = i.Id AND d.IsActive = 1
                   WHERE d.DeviceTypeId IS NULL)
            THROW 50013, N'Списъкът съдържа несъществуващ или неактивен вид устройство.', 1;

        DELETE FROM dbo.TechnicianDeviceTypes
        WHERE TechnicianId = @TechnicianId;

        INSERT INTO dbo.TechnicianDeviceTypes (TechnicianId, DeviceTypeId)
        SELECT @TechnicianId, Id
        FROM @DeviceTypeIds;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- „Кои заявки чакат оферти?“ Отворените заявки с вида устройство и броя чакащи оферти.
-- Без адрес и телефон на клиента. Филтърът по видовете устройства на техника е в API-то,
-- за да остане изгледът общ.
CREATE OR ALTER VIEW dbo.vw_OpenSupportRequests
AS
SELECT r.SupportRequestId,
       r.Title,
       r.Description,
       r.DeviceModel,
       r.City,
       r.PreferredDate,
       r.DeviceTypeId,
       d.Name     AS DeviceTypeName,
       u.FullName AS ClientName,
       r.CreatedAt,
       (SELECT COUNT(*)
        FROM dbo.Offers o
        WHERE o.SupportRequestId = r.SupportRequestId AND o.Status = N'Pending') AS OffersCount
FROM dbo.SupportRequests r
JOIN dbo.DeviceTypes d ON d.DeviceTypeId = r.DeviceTypeId
JOIN dbo.Users u       ON u.UserId = r.ClientId
WHERE r.Status = N'Open';
GO

-- „Какъв е този техник?“ Видове устройства, рейтинг, брой отзиви и завършени заявки.
-- Без телефон и адрес, без блокираните техници.
CREATE OR ALTER VIEW dbo.vw_TechnicianProfiles
AS
SELECT u.UserId AS TechnicianId,
       u.FullName,
       u.City,
       (SELECT STRING_AGG(d.Name, N', ') WITHIN GROUP (ORDER BY d.Name)
        FROM dbo.TechnicianDeviceTypes td
        JOIN dbo.DeviceTypes d ON d.DeviceTypeId = td.DeviceTypeId
        WHERE td.TechnicianId = u.UserId) AS DeviceTypes,
       u.AvgRating,
       u.ReviewsCount,
       (SELECT COUNT(*)
        FROM dbo.Offers o
        JOIN dbo.SupportRequests r ON r.SupportRequestId = o.SupportRequestId
        WHERE o.TechnicianId = u.UserId
          AND o.Status = N'Accepted'
          AND r.Status = N'Completed') AS CompletedCount
FROM dbo.Users u
WHERE u.Role = N'Technician' AND u.IsBlocked = 0;
GO


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261021051403_SqlObjects', N'10.0.12');
GO

PRINT N'Базата ITspec е създадена.';
