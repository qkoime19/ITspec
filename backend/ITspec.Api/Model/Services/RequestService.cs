using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Заявките: списък, детайли, създаване, прекратяване, завършване, отзив.</summary>
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

        if (!allowed)
        {
            throw AppException.Forbidden("Нямате достъп до тази заявка.");
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
            myOffer is null ? null : OfferService.ToDto(myOffer, myOffer.Technician.FullName, myOffer.Technician.AvgRating),
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
    /// Open или Assigned -> Cancelled. Само собственикът.
    /// Чакащите оферти стават Rejected. Едно SaveChanges = една транзакция.
    /// </summary>
    public async Task CancelAsync(int id, int clientId)
    {
        var request = await db.SupportRequests
            .Include(r => r.Offers)
            .SingleOrDefaultAsync(r => r.SupportRequestId == id)
            ?? throw AppException.NotFound("Заявката не съществува.");

        if (request.ClientId != clientId)
        {
            throw AppException.Forbidden("Можете да прекратите само своя заявка.");
        }

        if (request.Status is not (RequestStatus.Open or RequestStatus.Assigned))
        {
            throw AppException.Conflict("Само отворена или възложена заявка може да бъде прекратена.");
        }

        request.Status = RequestStatus.Cancelled;
        foreach (var offer in request.Offers.Where(o => o.Status == OfferStatus.Pending))
        {
            offer.Status = OfferStatus.Rejected;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Assigned -> Completed. Само техникът с приетата оферта.
    /// Проста промяна в една таблица, затова не е процедура.
    /// </summary>
    public async Task CompleteAsync(int id, int technicianId)
    {
        var request = await db.SupportRequests
            .Include(r => r.Offers)
            .SingleOrDefaultAsync(r => r.SupportRequestId == id)
            ?? throw AppException.NotFound("Заявката не съществува.");

        var accepted = request.Offers.SingleOrDefault(o => o.Status == OfferStatus.Accepted);
        if (accepted is null || accepted.TechnicianId != technicianId)
        {
            throw AppException.Forbidden("Само техникът с приетата оферта може да завърши заявката.");
        }

        if (request.Status != RequestStatus.Assigned)
        {
            throw AppException.Conflict("Само възложена заявка може да бъде завършена.");
        }

        request.Status = RequestStatus.Completed;
        await db.SaveChangesAsync();
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
