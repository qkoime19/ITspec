-- 04_test_data.sql
-- Тестови данни за 05_tests.sql. Всички тестови потребители са с имейл @test.local.
-- Скриптът може да се пуска многократно: първо изтрива старите тестови данни.
-- Пуска се СЛЕД Update-Database (таблиците, процедурите и изгледите вече съществуват).

USE ITspec;
GO
SET NOCOUNT ON;

-- 1. Изтриване на старите тестови данни (от децата към родителите, заради FOREIGN KEY)
DELETE rv FROM dbo.Reviews rv
JOIN dbo.SupportRequests r ON r.SupportRequestId = rv.SupportRequestId
JOIN dbo.Users u ON u.UserId = r.ClientId
WHERE u.Email LIKE N'%@test.local';

DELETE o FROM dbo.Offers o
JOIN dbo.Users u ON u.UserId = o.TechnicianId
WHERE u.Email LIKE N'%@test.local';

DELETE o FROM dbo.Offers o
JOIN dbo.SupportRequests r ON r.SupportRequestId = o.SupportRequestId
JOIN dbo.Users u ON u.UserId = r.ClientId
WHERE u.Email LIKE N'%@test.local';

DELETE r FROM dbo.SupportRequests r
JOIN dbo.Users u ON u.UserId = r.ClientId
WHERE u.Email LIKE N'%@test.local';

DELETE td FROM dbo.TechnicianDeviceTypes td
JOIN dbo.Users u ON u.UserId = td.TechnicianId
WHERE u.Email LIKE N'%@test.local';

DELETE FROM dbo.Users WHERE Email LIKE N'%@test.local';

-- 2. Потребители (паролата не е нужна за тестовете в базата)
INSERT INTO dbo.Users (Email, PasswordHash, FullName, Phone, Address, City, Role, IsBlocked)
VALUES (N'c1@test.local',      N'x', N'Тест Клиент 1',      N'0899000001', N'ул. Тестова 1', N'Враца', N'Client',     0),
       (N'c2@test.local',      N'x', N'Тест Клиент 2',      N'0899000002', N'ул. Тестова 2', N'Враца', N'Client',     0),
       (N't1@test.local',      N'x', N'Тест Техник 1',      N'0899000011', NULL,             N'Враца', N'Technician', 0),
       (N't2@test.local',      N'x', N'Тест Техник 2',      N'0899000012', NULL,             N'Враца', N'Technician', 0),
       (N't3@test.local',      N'x', N'Тест Техник 3',      N'0899000013', NULL,             N'Враца', N'Technician', 0),
       (N'busy@test.local',    N'x', N'Тест Зает Техник',   N'0899000014', NULL,             N'Враца', N'Technician', 0),
       (N'blocked@test.local', N'x', N'Тест Блокиран',      N'0899000015', NULL,             N'Враца', N'Technician', 1);

DECLARE @c1 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N'c1@test.local');
DECLARE @t1 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't1@test.local');
DECLARE @t2 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't2@test.local');
DECLARE @t3 INT      = (SELECT UserId FROM dbo.Users WHERE Email = N't3@test.local');
DECLARE @busy INT    = (SELECT UserId FROM dbo.Users WHERE Email = N'busy@test.local');
DECLARE @blocked INT = (SELECT UserId FROM dbo.Users WHERE Email = N'blocked@test.local');

-- 3. Видове устройства на техниците (1 = Лаптоп, 3 = Смартфон)
INSERT INTO dbo.TechnicianDeviceTypes (TechnicianId, DeviceTypeId)
VALUES (@t1, 1), (@t2, 1), (@t3, 3), (@busy, 1), (@blocked, 1);

-- 4. Заявки на клиент 1
INSERT INTO dbo.SupportRequests (ClientId, DeviceTypeId, Title, Description, City, Status)
VALUES (@c1, 1, N'T-R1 лаптоп', N'Тестова заявка за приемане и отзив', N'Враца', N'Open'),
       (@c1, 1, N'T-B1 зает',   N'Поета от заетия техник',             N'Враца', N'Assigned'),
       (@c1, 1, N'T-B2 зает',   N'Поета от заетия техник',             N'Враца', N'Assigned'),
       (@c1, 1, N'T-B3 зает',   N'Поета от заетия техник',             N'Враца', N'Assigned'),
       (@c1, 1, N'T-B4 четвърта', N'Четвърта заявка за заетия техник', N'Враца', N'Open');

-- 5. Оферти
INSERT INTO dbo.Offers (SupportRequestId, TechnicianId, Price, Message, Status)
SELECT r.SupportRequestId, o.TechnicianId, o.Price, o.Message, o.Status
FROM (VALUES (N'T-R1 лаптоп',   @t1,   60.00, N'Оферта на техник 1', N'Pending'),
             (N'T-R1 лаптоп',   @t2,   45.00, N'Оферта на техник 2', N'Pending'),
             (N'T-B1 зает',     @busy, 30.00, NULL,                  N'Accepted'),
             (N'T-B2 зает',     @busy, 30.00, NULL,                  N'Accepted'),
             (N'T-B3 зает',     @busy, 30.00, NULL,                  N'Accepted'),
             (N'T-B4 четвърта', @busy, 30.00, NULL,                  N'Pending')) AS o(Title, TechnicianId, Price, Message, Status)
JOIN dbo.SupportRequests r ON r.Title = o.Title AND r.ClientId = @c1;

PRINT N'Тестовите данни са готови. Пуснете 05_tests.sql.';
GO
