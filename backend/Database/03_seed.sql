-- 03_seed.sql
-- Началните данни също идват от миграцията InitialCreate (HasData в AppDbContext.SeedData):
--   6 вида устройства и администратор admin@itspec.local / Admin123!.
-- Демо клиентите и техниците (паролa Demo123!) се създават от Model/Data/DbSeeder.cs
-- при стартиране в Development.
--
-- Проверка след Update-Database:
USE ITspec;
GO
SELECT DeviceTypeId, Name, IsActive FROM dbo.DeviceTypes ORDER BY DeviceTypeId;
SELECT UserId, Email, Role FROM dbo.Users WHERE Role = N'Admin';
GO
