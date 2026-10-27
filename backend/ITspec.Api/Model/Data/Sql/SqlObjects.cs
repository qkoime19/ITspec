using Microsoft.EntityFrameworkCore.Migrations;

namespace ITspec.Api.Model.Data.Sql;

/// <summary>
/// SQL кодът на типа, процедурите и изгледите. Таблиците ги създава EF от AppDbContext,
/// а тези обекти — миграцията SqlObjects чрез migrationBuilder.Sql(...).
/// Всеки обект е отделен низ, защото CREATE PROCEDURE и CREATE VIEW трябва да са
/// първата команда в своя batch. Същият код е и в Database/02_objects.sql.
/// </summary>
public static class SqlObjects
{
    public const string IdListType = """
        -- Потребителски табличен тип: списък от Id като един параметър.
        -- Първичният ключ автоматично отхвърля повторени стойности.
        CREATE TYPE dbo.IdListType AS TABLE
        (
            Id INT NOT NULL PRIMARY KEY
        );
        """;

    public const string AcceptOffer = """
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
        """;

    public const string AddReview = """
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
        """;

    public const string SetTechnicianDeviceTypes = """
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
        """;

    public const string OpenSupportRequestsView = """
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
        """;

    public const string TechnicianProfilesView = """
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
        """;

    /// <summary>Извиква се в Up() на миграцията SqlObjects.</summary>
    public static void Create(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(IdListType);
        migrationBuilder.Sql(AcceptOffer);
        migrationBuilder.Sql(AddReview);
        migrationBuilder.Sql(SetTechnicianDeviceTypes);
        migrationBuilder.Sql(OpenSupportRequestsView);
        migrationBuilder.Sql(TechnicianProfilesView);
    }

    /// <summary>Извиква се в Down(). Обратен ред: типът е последен, защото процедурата го използва.</summary>
    public static void Drop(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_TechnicianProfiles;");
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_OpenSupportRequests;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_SetTechnicianDeviceTypes;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_AddReview;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_AcceptOffer;");
        migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.IdListType;");
    }
}
