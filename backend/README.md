# ITspec — back-end

REST API за платформата „IT помощ — компютри и телефони“: клиенти публикуват заявки за помощ с устройство, техници дават оферти, клиентът приема една, а след работата оставя отзив.

**Технологии:** ASP.NET Core Web API (.NET 10) · EF Core (Code First) · MSSQL (LocalDB) · JWT · Swagger UI

## Съдържание на папката

```
backend/
├── README.md                      ← този файл
├── Database/                      ← SQL скриптове, виж Database/README.md
│   ├── 01_schema.sql, 03_seed.sql     само за справка: таблиците и началните данни идват от миграциите
│   ├── 02_objects.sql                 процедурите, типът и изгледите (същият код е в миграцията SqlObjects)
│   ├── 04_test_data.sql               тестови данни (@test.local)
│   └── 05_tests.sql                   19 автоматични теста на правилата
├── ITspec.Api/                    ← самото приложение (+ front-end в wwwroot)
└── ITspec.Tests/                  ← xUnit (по-късно)
```

## Пускане за първи път

**Нужно е:** .NET 10 SDK, Visual Studio с „ASP.NET and web development“ и „Data storage and processing“ (дава LocalDB), SSMS.

1. **Connection string.** В `appsettings.json` вече има:
   `"Default": "Server=(localdb)\\MSSQLLocalDB;Database=ITspec;Trusted_Connection=True;TrustServerCertificate=True"`.
   Двойната `\\` е задължителна: в JSON `\` е специален знак.
2. **JWT ключ.** На този компютър ключът е във файла `ITspec.Api/appsettings.Local.json`, който е в `.gitignore` и не отива в Git.
   На друг компютър (напр. в кабинета) създайте същия файл: `{ "Jwt": { "Key": "<поне 32 случайни символа>" } }`
   или използвайте десен бутон върху проекта → **Manage User Secrets** → `{ "Jwt:Key": "..." }`. Без ключ приложението спира при старт с ясно съобщение.
3. **Миграциите вече са създадени** в `Model/Data/Migrations` (Code First — таблиците не се пишат в SSMS):
   - `InitialCreate` — генерирана от `AppDbContext`: 6-те таблици, ограниченията, видовете устройства и администратора;
   - `SqlObjects` — генерирана празна, а `Up`/`Down` са попълнени на ръка с `SqlCode.Create(...)` / `SqlCode.Drop(...)` (кодът е в `Model/Data/Sql/SqlObjects.cs`).
     Псевдонимът `SqlCode` е нужен, защото самата миграция също се казва `SqlObjects`.

   Как са създадени (за да можете да ги пресъздадете): Package Manager Console →
   ```
   Add-Migration InitialCreate -OutputDir Model/Data/Migrations
   Add-Migration SqlObjects -OutputDir Model/Data/Migrations
   ```
4. **База.** При старт в Development приложението само прилага миграциите (`MigrateAsync` в `Program.cs` — същото като `Update-Database`), така че базата `ITspec` се създава при първото пускане. Може и ръчно: `Update-Database`.
   В SSMS → `(localdb)\MSSQLLocalDB` → Refresh: 6 таблици, 3 процедури, 2 изгледа, тип `IdListType` и `__EFMigrationsHistory` с 2 реда.
5. **Старт.** Профил **https** → Ctrl+F5. Отваря се `https://localhost:7095/swagger`. Front-end-ът е на `https://localhost:7095/`.

При първия старт в Development се създават демо потребители (`Model/Data/DbSeeder.cs`):

| Имейл | Роля | Видове устройства |
|---|---|---|
| admin@itspec.local (парола `Admin123!`) | Admin | — |
| maria@itspec.local, petar@itspec.local | Client | — |
| ivan@itspec.local | Technician | Лаптоп, Настолен компютър |
| georgi@itspec.local | Technician | Смартфон, Таблет |
| nikola@itspec.local | Technician | Лаптоп, Смартфон, Принтер, Рутер и мрежа |

Паролата на демо потребителите е `Demo123!` (`appsettings.Development.json`, `Seed:DemoPassword`). Администраторът идва от миграцията, не от DbSeeder.

## Структура на проекта (MVC)

| Папка | Слой | Какво има |
|---|---|---|
| `Controllers/` | C | Тънки контролери: приемат заявката, проверяват ролята, викат Service, връщат отговор. Без `try/catch`. |
| `Model/Entities/` | M | Класовете за таблиците и изгледите, enum-ите за статусите, ролите |
| `Model/Data/` | M | `AppDbContext`, `DbSeeder`, миграциите, `Sql/SqlObjects.cs` |
| `Model/Services/` | M | Бизнес правилата: кой какво може, кога 403/404/409 |
| `View/DTOs/` | V | Какво влиза и излиза от API-то. Entity клас никога не се връща. |
| `wwwroot/` | V | Front-end: чист HTML, CSS и JavaScript, `js/api.js` |
| `Infrastructure/Auth/` | — | JWT токени и имена на claims |
| `Infrastructure/Errors/` | — | `AppException` и `ApiExceptionHandler`: едно място, където грешка става HTTP отговор |
| `Infrastructure/OpenApi/` | — | Бутонът **Authorize** в Swagger |

## Обекти в базата

| Обект | Вид | Правило, заради което съществува |
|---|---|---|
| `usp_AcceptOffer` | процедура + транзакция | точно една приета оферта; останалите се отхвърлят; заявката става Assigned; **собствено правило:** най-много 3 поети заявки на техник |
| `usp_AddReview` | процедура + транзакция | отзив веднъж, за своя завършена заявка; рейтингът на техника се преизчислява |
| `usp_SetTechnicianDeviceTypes` | процедура + табличен тип | видовете устройства се сменят изцяло или изобщо не |
| `dbo.IdListType` | табличен тип | списък от Id като един параметър |
| `vw_OpenSupportRequests` | изглед | „кои заявки чакат оферти?“, без адрес и телефон |
| `vw_TechnicianProfiles` | изглед | „какъв е този техник?“, без телефон и без блокираните |

## Тестове

| Къде | Какво | Как |
|---|---|---|
| `Database/05_tests.sql` | 19 теста на правилата в базата | в SSMS, след `04_test_data.sql` → очаквано 19 PASS |
| `ITspec.Api/ITspec.Api.http` | 54 сценария през HTTP | във Visual Studio, „Send request“ над всяка заявка, по ред; очакваният код е написан над всяка |
| Swagger | ръчни опити | `/swagger` → login → копирайте `token` → **Authorize** |
| `docs/test-matrix.md` | тестовата матрица | очакваните кодове са попълнени; попълнете реалните |

## Чести проблеми

| Симптом | Причина | Решение |
|---|---|---|
| „Липсва Jwt:Key…“ при старт или при Add-Migration | няма User Secrets или ключът е под 32 символа | Manage User Secrets |
| SqlException в `DbSeeder.cs` при старт | няма база или LocalDB е спрян | `Update-Database` или `sqllocaldb start MSSQLLocalDB` |
| „Invalid object name 'vw_…'“ или „Could not find stored procedure“ | миграцията SqlObjects е празна | попълнете Up/Down (стъпка 4), `Update-Database` |
| „There is already an object named …“ при `Update-Database` | базата е създадена ръчно | `Drop-Database`, после `Update-Database` |
| ERR_CONNECTION_REFUSED | API-то не работи: Build с грешки или срив при старт | Error List и конзолата |

## Известни ограничения (за дискусия)

- **Блокиран потребител със стар токен** продължава да работи до изтичането на токена (120 минути).
- **React front-end** (бонус) не е направен: по заданието се прави едва когато вариантът с чист JavaScript работи по целия цикъл.
