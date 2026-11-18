using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Заявките: списък, детайли, създаване, редакция, прекратяване, завършване, отзив.</summary>
public class RequestService(AppDbContext db)
{
    /// <summary>
    /// Списъкът зависи от ролята:
    ///   Client     -> своите заявки (всички статуси);
    ///   Technician -> отворените заявки в своите видове устройства (от изгледа vw_OpenSupportRequests);
    ///   Admin      -> всички заявки.
    /// </summary>
    public async Task<List<RequestListItemDto>> GetListAsync(int userId, string role)
    {
        if (role == Roles.Technician)
        {
            var myTypeIds = db.TechnicianDeviceTypes
                .Where(t => t.TechnicianId == userId)
                .Select(t => t.DeviceTypeId);

            return await db.OpenSupportRequests
                .Where(v => myTypeIds.Contains(v.DeviceTypeId))
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new RequestListItemDto(
                    v.SupportRequestId, v.Title, v.DeviceTypeName, v.DeviceModel, v.City, v.PreferredDate,
                    RequestStatus.Open, v.CreatedAt, v.OffersCount, v.ClientName))
                .ToListAsync();
        }

        var query = db.SupportRequests.AsQueryable();
        if (role == Roles.Client)
        {
            query = query.Where(r => r.ClientId == userId);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RequestListItemDto(
                r.SupportRequestId,
                r.Title,
                r.DeviceType.Name,
                r.DeviceModel,
                r.City,
                r.PreferredDate,
                r.Status,
                r.CreatedAt,
                r.Offers.Count(o => o.Status != OfferStatus.Withdrawn),
                r.Client.FullName))
            .ToListAsync();
    }

    /// <summary>
    /// „Моите заявки“ на клиента: винаги филтър по собственик (от токена),
    /// незадължителен филтър по статус, подредба от най-новите, страници по 20.
    /// OrderBy е преди Skip/Take, иначе страниците не са стабилни.
    /// </summary>
    public async Task<List<RequestListItemDto>> GetMineAsync(int clientId, RequestStatus? status, int page)
    {
        var query = db.SupportRequests.Where(r => r.ClientId == clientId);
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((Math.Max(page, 1) - 1) * PageSize)
            .Take(PageSize)
            .Select(r => new RequestListItemDto(
                r.SupportRequestId, r.Title, r.DeviceType.Name, r.DeviceModel, r.City, r.PreferredDate,
                r.Status, r.CreatedAt, r.Offers.Count(o => o.Status != OfferStatus.Withdrawn), r.Client.FullName))
            .ToListAsync();
    }

    public const int PageSize = 20;

    /// <summary>
    /// Публичната „дъска с обяви“: отворените заявки от изгледа vw_OpenSupportRequests,
    /// с филтри по вид устройство, град и текст. Вижда се и без вход.
    /// Изгледът няма адрес и телефон, затова тук скритите данни не могат да излязат.
    /// </summary>
    public async Task<List<RequestListItemDto>> GetOpenPublicAsync(int? deviceTypeId, string? city, string? search)
    {
        var query = db.OpenSupportRequests.AsQueryable();

        if (deviceTypeId is not null)
        {
            query = query.Where(v => v.DeviceTypeId == deviceTypeId);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim();
            query = query.Where(v => v.City.Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            query = query.Where(v => v.Title.Contains(text)
                                     || v.Description.Contains(text)
                                     || (v.DeviceModel != null && v.DeviceModel.Contains(text)));
        }

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .Take(100)
            .Select(v => new RequestListItemDto(
                v.SupportRequestId, v.Title, v.DeviceTypeName, v.DeviceModel, v.City, v.PreferredDate,
                RequestStatus.Open, v.CreatedAt, v.OffersCount, v.ClientName))
            .ToListAsync();
    }

    public async Task<RequestDetailsDto> GetDetailsAsync(int id, int userId, string role)
    {
        var request = await db.SupportRequests
            .AsNoTracking()
            .Include(r => r.DeviceType)
            .Include(r => r.Client)
            .Include(r => r.Review)
            .Include(r => r.Offers).ThenInclude(o => o.Technician)
            .SingleOrDefaultAsync(r => r.SupportRequestId == id)
            ?? throw AppException.NotFound("Заявката не съществува.");

        var myOffer = role == Roles.Technician
            ? request.Offers.SingleOrDefault(o => o.TechnicianId == userId)
            : null;

        bool allowed;
        if (role == Roles.Admin)
        {
            allowed = true;
        }
        else if (role == Roles.Client)
        {
            allowed = request.ClientId == userId;
        }
        else if (role == Roles.Technician)
        {
            // Техникът вижда заявката, ако е дал оферта по нея
            // или ако е отворена и е в неговите видове устройства.
            allowed = myOffer is not null
                      || (request.Status == RequestStatus.Open
                          && await IsInTechnicianDeviceTypesAsync(userId, request.DeviceTypeId));
        }
        else
        {
            allowed = false;
        }

        // Чужд запис -> 404, не 403: отговорът не издава, че заявката съществува.
        if (!allowed)
        {
            throw AppException.NotFound("Заявката не съществува.");
        }

        var accepted = request.Offers.SingleOrDefault(o => o.Status == OfferStatus.Accepted);

        // Скрити данни: адресът и телефонът на клиента излизат САМО към техника с приетата оферта.
        var showContact = role == Roles.Technician && accepted is not null && accepted.TechnicianId == userId;

        return new RequestDetailsDto(
            request.SupportRequestId,
            request.Title,
            request.Description,
            request.DeviceModel,
            request.DeviceTypeId,
            request.DeviceType.Name,
            request.City,
            request.PreferredDate,
            request.Status,
            request.CreatedAt,
            request.ClientId,
            request.Client.FullName,
            showContact ? request.Client.Phone : null,
            showContact ? request.Client.Address : null,
            accepted is null
                ? null
                : new AcceptedOfferDto(accepted.OfferId, accepted.TechnicianId, accepted.Technician.FullName,
                    accepted.Price, accepted.ProposedDate),
            myOffer is null ? null : OfferDto.From(myOffer, myOffer.Technician.FullName, myOffer.Technician.AvgRating),
            request.Review is null
                ? null
                : new ReviewDto(request.Review.ReviewId, request.Review.Rating, request.Review.Comment, request.Review.CreatedAt));
    }

    public async Task<int> CreateAsync(int clientId, CreateRequestDto dto)
    {
        var typeIsActive = await db.DeviceTypes.AnyAsync(d => d.DeviceTypeId == dto.DeviceTypeId && d.IsActive);
        if (!typeIsActive)
        {
            throw AppException.BadRequest("Изберете съществуващ и активен вид устройство.");
        }

        var client = await db.Users.FindAsync(clientId)
            ?? throw AppException.NotFound("Потребителят не съществува.");

        var request = new SupportRequest
        {
            ClientId = clientId,
            DeviceTypeId = dto.DeviceTypeId,
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            DeviceModel = string.IsNullOrWhiteSpace(dto.DeviceModel) ? null : dto.DeviceModel.Trim(),
            City = string.IsNullOrWhiteSpace(dto.City) ? client.City : dto.City.Trim(),
            PreferredDate = dto.PreferredDate,
            Status = RequestStatus.Open
        };

        db.SupportRequests.Add(request);
        await db.SaveChangesAsync();
        return request.SupportRequestId;
    }

    /// <summary>
    /// Редакция на съдържанието (Open -> Open). Само собственикът и само докато заявката е Open.
    /// ExecuteUpdateAsync прави един UPDATE ... WHERE Id AND ClientId AND Status = 'Open',
    /// без да зарежда записа. Собственикът и статусът са в WHERE, затова между
    /// проверката и записа никой не може да „промъкне“ промяна на статуса.
    /// При 0 променени реда AnyAsync решава коя е грешката:
    ///   заявката не е моя или не съществува -> 404 (не издаваме чужди заявки);
    ///   моя е, но вече не е Open           -> 409.
    /// </summary>
    public async Task UpdateAsync(int id, int clientId, UpdateRequestDto dto)
    {
        var typeIsActive = await db.DeviceTypes.AnyAsync(d => d.DeviceTypeId == dto.DeviceTypeId && d.IsActive);
        if (!typeIsActive)
        {
            throw AppException.BadRequest("Изберете съществуващ и активен вид устройство.");
        }

        var title = dto.Title.Trim();
        var description = dto.Description.Trim();
        var deviceModel = string.IsNullOrWhiteSpace(dto.DeviceModel) ? null : dto.DeviceModel.Trim();
        var city = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();

        var changed = await db.SupportRequests
            .Where(r => r.SupportRequestId == id && r.ClientId == clientId && r.Status == RequestStatus.Open)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.DeviceTypeId, dto.DeviceTypeId)
                .SetProperty(r => r.Title, title)
                .SetProperty(r => r.Description, description)
                .SetProperty(r => r.DeviceModel, deviceModel)
                .SetProperty(r => r.City, r => city ?? r.City)
                .SetProperty(r => r.PreferredDate, dto.PreferredDate));

        if (changed == 0)
        {
            var isMine = await db.SupportRequests.AnyAsync(r => r.SupportRequestId == id && r.ClientId == clientId);
            throw isMine
                ? AppException.Conflict("Заявката може да се редактира само докато е отворена.")
                : AppException.NotFound("Заявката не съществува.");
        }
    }

    /// <summary>
    /// Open или Assigned -> Cancelled. Само собственикът.
    /// Две промени (заявката + чакащите оферти) -> обща транзакция: всичко или нищо.
    /// Условията (собственик и статус) са в WHERE на UPDATE-а, затова никой не може да смени
    /// статуса между проверката и записа. 0 реда: моя е -> 409 (грешен статус), иначе -> 404.
    /// </summary>
    public async Task CancelAsync(int id, int clientId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var changed = await db.SupportRequests
            .Where(r => r.SupportRequestId == id && r.ClientId == clientId
                        && (r.Status == RequestStatus.Open || r.Status == RequestStatus.Assigned))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Cancelled));

        if (changed == 0)
        {
            var isMine = await db.SupportRequests.AnyAsync(r => r.SupportRequestId == id && r.ClientId == clientId);
            throw isMine
                ? AppException.Conflict("Само отворена или възложена заявка може да бъде прекратена.")
                : AppException.NotFound("Заявката не съществува.");
        }

        await db.Offers
            .Where(o => o.SupportRequestId == id && o.Status == OfferStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OfferStatus.Rejected));

        await tx.CommitAsync();
    }

    /// <summary>
    /// Assigned -> Completed. Само техникът с приетата оферта.
    /// Проста промяна в една таблица, затова не е процедура: един UPDATE с условие.
    /// 0 реда: това е моя приета работа, но не е Assigned -> 409; иначе (чужда или няма я) -> 404.
    /// </summary>
    public async Task CompleteAsync(int id, int technicianId)
    {
        var changed = await db.SupportRequests
            .Where(r => r.SupportRequestId == id
                        && r.Status == RequestStatus.Assigned
                        && r.Offers.Any(o => o.Status == OfferStatus.Accepted && o.TechnicianId == technicianId))
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Completed));

        if (changed == 0)
        {
            var isMyJob = await db.Offers.AnyAsync(o => o.SupportRequestId == id
                                                        && o.TechnicianId == technicianId
                                                        && o.Status == OfferStatus.Accepted);
            throw isMyJob
                ? AppException.Conflict("Само възложена заявка може да бъде завършена.")
                : AppException.NotFound("Заявката не съществува.");
        }
    }

    /// <summary>
    /// Отзив: процедурата usp_AddReview проверява правилата и в една транзакция
    /// записва отзива и обновява рейтинга на техника. ClientId идва от токена.
    /// </summary>
    public async Task<ReviewDto> AddReviewAsync(int id, int clientId, CreateReviewDto dto)
    {
        // Празен коментар се праща като "" — процедурата го записва като NULL.
        var comment = dto.Comment?.Trim() ?? string.Empty;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.usp_AddReview @SupportRequestId = {id}, @ClientId = {clientId}, @Rating = {dto.Rating}, @Comment = {comment}");

        return await db.Reviews
            .Where(r => r.SupportRequestId == id)
            .Select(r => new ReviewDto(r.ReviewId, r.Rating, r.Comment, r.CreatedAt))
            .SingleAsync();
    }

    public Task<bool> IsInTechnicianDeviceTypesAsync(int technicianId, int deviceTypeId)
    {
        return db.TechnicianDeviceTypes
            .AnyAsync(t => t.TechnicianId == technicianId && t.DeviceTypeId == deviceTypeId);
    }
}
