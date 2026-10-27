# 2. Таблици и връзки

Таблиците се описват с C# класове в `Model/Entities` и се създават от EF Core (Code First) с миграцията `InitialCreate`. В SSMS не се пише `CREATE TABLE`.

| Таблица (клас) | Основни колони | Ограничения |
| --- | --- | --- |
| Users (`User`) | UserId, Email, PasswordHash, FullName, Phone, Address, City, Role, IsBlocked, AvgRating, ReviewsCount, CreatedAt | Email UNIQUE; CHECK Role IN ('Admin','Client','Technician'); CHECK AvgRating 1–5 |
| DeviceTypes (`DeviceType`) | DeviceTypeId, Name, IsActive | Name UNIQUE |
| TechnicianDeviceTypes (`TechnicianDeviceType`) | TechnicianId, DeviceTypeId | съставен ключ (връзка N:M) |
| SupportRequests (`SupportRequest`) | SupportRequestId, ClientId, DeviceTypeId, Title, Description, DeviceModel, City, PreferredDate, Status, CreatedAt | CHECK Status IN (Open, Assigned, Completed, Cancelled) |
| Offers (`Offer`) | OfferId, SupportRequestId, TechnicianId, Price, Message, ProposedDate, Status, CreatedAt | UNIQUE (SupportRequestId, TechnicianId); филтриран UNIQUE (SupportRequestId) WHERE Status = 'Accepted'; CHECK Price > 0 |
| Reviews (`Review`) | ReviewId, SupportRequestId, Rating, Comment, CreatedAt | SupportRequestId UNIQUE; CHECK Rating 1–5 |

## Връзки

| Връзка | Вид | Как се превежда |
| --- | --- | --- |
| Клиент → Заявки | 1:N | ClientId в SupportRequests |
| Вид устройство → Заявки | 1:N | DeviceTypeId в SupportRequests |
| Техници ↔ Видове устройства | N:M | междинна таблица TechnicianDeviceTypes |
| Заявка → Оферти | 1:N | SupportRequestId в Offers |
| Техник → Оферти | 1:N | TechnicianId в Offers |
| Заявка → Отзив | 1:0..1 | SupportRequestId в Reviews, уникален |

## Решения

- **Ролята е колона, не таблица.** Заданието го изисква; допустимите стойности се пазят с CHECK.
- **Няма таблица „работа“.** Приетата оферта казва кой, кога и за колко; статусът на заявката казва дали е завършена.
- **AvgRating и ReviewsCount в Users са съзнателна денормализация.** Изчисляват се от `usp_AddReview` в същата транзакция, в която се записва отзивът, за да не се разминават.
- **Никъде няма каскадно изтриване.** Потребителите се блокират, видовете устройства се деактивират, заявките се прекратяват.
- **Скритите данни** (адрес и телефон на клиента) са в Users и не присъстват в нито един изглед.
