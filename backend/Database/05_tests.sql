-- 05_tests.sql
-- Автоматични тестове на бизнес правилата в базата.
-- Всеки тест се опитва да НАРУШИ правило и очаква базата да откаже с конкретна грешка.
-- Ред: Update-Database -> 04_test_data.sql -> 05_tests.sql. Очакван резултат: 19 PASS, 0 FAIL.
-- Тестовете променят тестовите данни, затова преди повторно пускане пуснете пак 04_test_data.sql.

USE ITspec;
GO
SET NOCOUNT ON;

DECLARE @Results TABLE (No INT, Test NVARCHAR(200), Result NVARCHAR(10), Details NVARCHAR(400));
DECLARE @err INT, @msg NVARCHAR(400);

DECLARE @c1 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N'c1@test.local');
DECLARE @c2 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N'c2@test.local');
DECLARE @t1 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't1@test.local');
DECLARE @t2 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't2@test.local');
DECLARE @t3 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't3@test.local');
DECLARE @busy INT    = (SELECT UserId FROM dbo.Users WHERE Email = N'busy@test.local');
DECLARE @r1 INT      = (SELECT SupportRequestId FROM dbo.SupportRequests WHERE Title = N'T-R1 лаптоп' AND ClientId = @c1);
DECLARE @b4 INT      = (SELECT SupportRequestId FROM dbo.SupportRequests WHERE Title = N'T-B4 четвърта' AND ClientId = @c1);
DECLARE @o1 INT      = (SELECT OfferId FROM dbo.Offers WHERE SupportRequestId = @r1 AND TechnicianId = @t1);
DECLARE @o2 INT      = (SELECT OfferId FROM dbo.Offers WHERE SupportRequestId = @r1 AND TechnicianId = @t2);
DECLARE @ob4 INT     = (SELECT OfferId FROM dbo.Offers WHERE SupportRequestId = @b4 AND TechnicianId = @busy);

IF @r1 IS NULL OR @o1 IS NULL OR @ob4 IS NULL
BEGIN
    RAISERROR(N'Няма тестови данни. Пуснете първо 04_test_data.sql.', 16, 1);
    RETURN;
END

------------------------------------------------------------------------------------------
-- T1. Имейлът е уникален (UNIQUE индекс) -> грешка 2601
BEGIN TRY
    INSERT INTO dbo.Users (Email, PasswordHash, FullName, City, Role)
    VALUES (N'c1@test.local', N'x', N'Дубликат', N'Враца', N'Client');
    INSERT INTO @Results VALUES (1, N'Имейлът е уникален', N'FAIL', N'Вторият запис мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (1, N'Имейлът е уникален',
        IIF(ERROR_NUMBER() IN (2601, 2627), N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T2. Ролята е Client, Technician или Admin (CHECK) -> грешка 547
BEGIN TRY
    INSERT INTO dbo.Users (Email, PasswordHash, FullName, City, Role)
    VALUES (N'boss@test.local', N'x', N'Шеф', N'Враца', N'Boss');
    INSERT INTO @Results VALUES (2, N'Невалидна роля се отказва', N'FAIL', N'Ролята Boss мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (2, N'Невалидна роля се отказва', IIF(ERROR_NUMBER() = 547, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T3. Един техник дава една оферта на заявка (UNIQUE) -> 2601
BEGIN TRY
    INSERT INTO dbo.Offers (SupportRequestId, TechnicianId, Price) VALUES (@r1, @t1, 10);
    INSERT INTO @Results VALUES (3, N'Една оферта на техник по заявка', N'FAIL', N'Втората оферта мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (3, N'Една оферта на техник по заявка',
        IIF(ERROR_NUMBER() IN (2601, 2627), N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T4. Цената е положителна (CHECK) -> 547
BEGIN TRY
    INSERT INTO dbo.Offers (SupportRequestId, TechnicianId, Price) VALUES (@r1, @t3, 0);
    INSERT INTO @Results VALUES (4, N'Цената е положителна', N'FAIL', N'Цена 0 мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (4, N'Цената е положителна', IIF(ERROR_NUMBER() = 547, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T5. Статусът на заявка е от четирите (CHECK) -> 547
BEGIN TRY
    UPDATE dbo.SupportRequests SET Status = N'Done' WHERE SupportRequestId = @r1;
    INSERT INTO @Results VALUES (5, N'Невалиден статус на заявка', N'FAIL', N'Статус Done мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (5, N'Невалиден статус на заявка', IIF(ERROR_NUMBER() = 547, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T6. Оценката е от 1 до 5 (CHECK) -> 547
BEGIN TRY
    INSERT INTO dbo.Reviews (SupportRequestId, Rating) VALUES (@r1, 7);
    INSERT INTO @Results VALUES (6, N'Оценката е от 1 до 5 (CHECK)', N'FAIL', N'Оценка 7 мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (6, N'Оценката е от 1 до 5 (CHECK)', IIF(ERROR_NUMBER() = 547, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T7. Най-много една приета оферта на заявка (филтриран уникален индекс) -> 2601
BEGIN TRY
    UPDATE dbo.Offers SET Status = N'Accepted' WHERE OfferId IN (@o1, @o2);
    INSERT INTO @Results VALUES (7, N'Една приета оферта (индекс)', N'FAIL', N'Две приети оферти минаха');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (7, N'Една приета оферта (индекс)',
        IIF(ERROR_NUMBER() IN (2601, 2627), N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T8. Само собственикът приема оферта (usp_AcceptOffer) -> 50002 и нищо не се променя
BEGIN TRY
    EXEC dbo.usp_AcceptOffer @OfferId = @o1, @ClientId = @c2;
    INSERT INTO @Results VALUES (8, N'Чужд клиент не приема оферта', N'FAIL', N'Процедурата не върна грешка');
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER(); SET @msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES (8, N'Чужд клиент не приема оферта',
        IIF(@err = 50002
            AND (SELECT Status FROM dbo.Offers WHERE OfferId = @o1) = N'Pending'
            AND (SELECT Status FROM dbo.SupportRequests WHERE SupportRequestId = @r1) = N'Open', N'PASS', N'FAIL'), @msg);
END CATCH

-- T9. Приемане от собственика: офертата Accepted, другата Rejected, заявката Assigned (всичко заедно)
BEGIN TRY
    EXEC dbo.usp_AcceptOffer @OfferId = @o1, @ClientId = @c1;
    INSERT INTO @Results VALUES (9, N'Приемане: 3 промени в една транзакция',
        IIF((SELECT Status FROM dbo.Offers WHERE OfferId = @o1) = N'Accepted'
            AND (SELECT Status FROM dbo.Offers WHERE OfferId = @o2) = N'Rejected'
            AND (SELECT Status FROM dbo.SupportRequests WHERE SupportRequestId = @r1) = N'Assigned', N'PASS', N'FAIL'), NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (9, N'Приемане: 3 промени в една транзакция', N'FAIL', ERROR_MESSAGE());
END CATCH

-- T10. Втора приета оферта по същата заявка -> 50003 (заявката вече не е Open)
BEGIN TRY
    EXEC dbo.usp_AcceptOffer @OfferId = @o2, @ClientId = @c1;
    INSERT INTO @Results VALUES (10, N'Втора приета оферта се отказва', N'FAIL', N'Процедурата не върна грешка');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (10, N'Втора приета оферта се отказва', IIF(ERROR_NUMBER() IN (50003, 50004), N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T11. Отзив за заявка, която не е Completed -> 50009
BEGIN TRY
    EXEC dbo.usp_AddReview @SupportRequestId = @r1, @ClientId = @c1, @Rating = 5, @Comment = N'рано';
    INSERT INTO @Results VALUES (11, N'Отзив само за Completed', N'FAIL', N'Отзивът мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (11, N'Отзив само за Completed', IIF(ERROR_NUMBER() = 50009, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- Завършване (в API-то това е POST /requests/{id}/complete)
UPDATE dbo.SupportRequests SET Status = N'Completed' WHERE SupportRequestId = @r1;

-- T12. Оценка извън 1-5 през процедурата -> 50007
BEGIN TRY
    EXEC dbo.usp_AddReview @SupportRequestId = @r1, @ClientId = @c1, @Rating = 9, @Comment = NULL;
    INSERT INTO @Results VALUES (12, N'Оценка 9 се отказва (процедура)', N'FAIL', N'Отзивът мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (12, N'Оценка 9 се отказва (процедура)', IIF(ERROR_NUMBER() = 50007, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T13. Отзив от чужд клиент -> 50008
BEGIN TRY
    EXEC dbo.usp_AddReview @SupportRequestId = @r1, @ClientId = @c2, @Rating = 1, @Comment = NULL;
    INSERT INTO @Results VALUES (13, N'Чужд клиент не оставя отзив', N'FAIL', N'Отзивът мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (13, N'Чужд клиент не оставя отзив', IIF(ERROR_NUMBER() = 50008, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T14. Валиден отзив: записан е и рейтингът на техника е обновен
BEGIN TRY
    EXEC dbo.usp_AddReview @SupportRequestId = @r1, @ClientId = @c1, @Rating = 4, @Comment = N'Добра работа';
    INSERT INTO @Results VALUES (14, N'Отзив + рейтинг в една транзакция',
        IIF(EXISTS (SELECT 1 FROM dbo.Reviews WHERE SupportRequestId = @r1 AND Rating = 4)
            AND (SELECT AvgRating FROM dbo.Users WHERE UserId = @t1) = 4.00
            AND (SELECT ReviewsCount FROM dbo.Users WHERE UserId = @t1) = 1, N'PASS', N'FAIL'), NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (14, N'Отзив + рейтинг в една транзакция', N'FAIL', ERROR_MESSAGE());
END CATCH

-- T15. Втори отзив за същата заявка -> 50010
BEGIN TRY
    EXEC dbo.usp_AddReview @SupportRequestId = @r1, @ClientId = @c1, @Rating = 5, @Comment = NULL;
    INSERT INTO @Results VALUES (15, N'Един отзив на заявка', N'FAIL', N'Вторият отзив мина');
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (15, N'Един отзив на заявка', IIF(ERROR_NUMBER() = 50010, N'PASS', N'FAIL'), ERROR_MESSAGE());
END CATCH

-- T16. Видове устройства с несъществуващ вид -> 50013, старите остават
DECLARE @bad dbo.IdListType;
INSERT INTO @bad VALUES (3), (999);
BEGIN TRY
    EXEC dbo.usp_SetTechnicianDeviceTypes @TechnicianId = @t3, @DeviceTypeIds = @bad;
    INSERT INTO @Results VALUES (16, N'Невалиден списък не сменя нищо', N'FAIL', N'Процедурата не върна грешка');
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER(); SET @msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES (16, N'Невалиден списък не сменя нищо',
        IIF(@err = 50013
            AND (SELECT COUNT(*) FROM dbo.TechnicianDeviceTypes WHERE TechnicianId = @t3) = 1
            AND EXISTS (SELECT 1 FROM dbo.TechnicianDeviceTypes WHERE TechnicianId = @t3 AND DeviceTypeId = 3), N'PASS', N'FAIL'), @msg);
END CATCH

-- T17. Валиден списък сменя всички видове наведнъж
DECLARE @good dbo.IdListType;
INSERT INTO @good VALUES (3), (4), (5);
BEGIN TRY
    EXEC dbo.usp_SetTechnicianDeviceTypes @TechnicianId = @t3, @DeviceTypeIds = @good;
    INSERT INTO @Results VALUES (17, N'Смяна на видовете през табличен тип',
        IIF((SELECT COUNT(*) FROM dbo.TechnicianDeviceTypes WHERE TechnicianId = @t3) = 3, N'PASS', N'FAIL'), NULL);
END TRY
BEGIN CATCH
    INSERT INTO @Results VALUES (17, N'Смяна на видовете през табличен тип', N'FAIL', ERROR_MESSAGE());
END CATCH

-- T18. СОБСТВЕНО ПРАВИЛО: техник с 3 поети заявки не може да поеме четвърта -> 50005
BEGIN TRY
    EXEC dbo.usp_AcceptOffer @OfferId = @ob4, @ClientId = @c1;
    INSERT INTO @Results VALUES (18, N'Собствено правило: най-много 3 поети заявки', N'FAIL', N'Четвъртата заявка беше приета');
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER(); SET @msg = ERROR_MESSAGE();
    INSERT INTO @Results VALUES (18, N'Собствено правило: най-много 3 поети заявки',
        IIF(@err = 50005
            AND (SELECT Status FROM dbo.SupportRequests WHERE SupportRequestId = @b4) = N'Open', N'PASS', N'FAIL'), @msg);
END CATCH

-- T19. Скрити данни: изгледите нямат колони Phone и Address; блокираният техник не е в профилите
INSERT INTO @Results VALUES (19, N'Изгледите не показват телефон и адрес; без блокирани',
    IIF(NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id IN (OBJECT_ID(N'dbo.vw_OpenSupportRequests'), OBJECT_ID(N'dbo.vw_TechnicianProfiles'))
                      AND name IN (N'Phone', N'Address'))
        AND NOT EXISTS (SELECT 1 FROM dbo.vw_TechnicianProfiles p
                        JOIN dbo.Users u ON u.UserId = p.TechnicianId
                        WHERE u.Email = N'blocked@test.local'), N'PASS', N'FAIL'), NULL);

------------------------------------------------------------------------------------------
SELECT No, Test, Result, Details FROM @Results ORDER BY No;

SELECT SUM(IIF(Result = N'PASS', 1, 0)) AS PASS,
       SUM(IIF(Result = N'FAIL', 1, 0)) AS FAIL
FROM @Results;
GO
