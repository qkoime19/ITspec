# Database

Таблиците **не се пишат в SSMS**. Те се описват с C# класове (`Model/Entities`) и `AppDbContext`, а EF Core ги създава с миграции (Code First). Тук са SQL скриптовете за четене и проверка.

| Файл | Какво има | Пуска ли се? |
| --- | --- | --- |
| `01_schema.sql` | как да видите SQL кода на таблиците, генериран от миграцията (`Script-Migration`) | не — таблиците идват от `InitialCreate` |
| `02_objects.sql` | табличен тип `IdListType`, процедури `usp_AcceptOffer`, `usp_AddReview`, `usp_SetTechnicianDeviceTypes`, изгледи `vw_OpenSupportRequests`, `vw_TechnicianProfiles` | не — същият код е в миграцията `SqlObjects` |
| `03_seed.sql` | проверка на началните данни (видове устройства, администратор) | по желание |
| `04_test_data.sql` | тестови данни с имейли `@test.local` | да, преди 05 |
| `05_tests.sql` | 19 автоматични теста на правилата | да → очаквано **19 PASS, 0 FAIL** |

Ред: `Update-Database` → `04_test_data.sql` → `05_tests.sql`. Тестовете променят тестовите данни, затова преди повторно пускане пуснете отново `04_test_data.sql`.

## Номера на грешките от процедурите

| Номер | Процедура | Кога | HTTP |
| --- | --- | --- | --- |
| 50001 | usp_AcceptOffer | офертата не съществува | 409 |
| 50002 | usp_AcceptOffer | заявката не е на този клиент | 409 |
| 50003 | usp_AcceptOffer | заявката не е Open | 409 |
| 50004 | usp_AcceptOffer | офертата не е Pending | 409 |
| 50005 | usp_AcceptOffer | **собствено правило:** техникът вече има 3 поети заявки | 409 |
| 50006 | usp_AddReview | заявката не съществува | 409 |
| 50007 | usp_AddReview | оценка извън 1–5 | 400 |
| 50008 | usp_AddReview | заявката не е на този клиент | 409 |
| 50009 | usp_AddReview | заявката не е Completed | 409 |
| 50010 | usp_AddReview | вече има отзив | 409 |
| 50011 | usp_SetTechnicianDeviceTypes | потребителят не е техник | 409 |
| 50012 | usp_SetTechnicianDeviceTypes | празен списък | 409 |
| 50013 | usp_SetTechnicianDeviceTypes | несъществуващ или неактивен вид | 409 |
