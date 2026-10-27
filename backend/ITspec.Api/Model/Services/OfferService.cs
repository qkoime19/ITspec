using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Офертите: даване, списък, оттегляне, приемане.</summary>
public class OfferService(AppDbContext db, RequestService requests)
{
    /// <summary>
    /// Техник дава оферта. Правила:
    ///   заявката е в неговите видове устройства (иначе 403);
    ///   заявката е Open (иначе 409);
    ///   една оферта на техник по заявка (иначе 409; пази и UNIQUE индексът).
    /// </summary>
    public async Task<OfferDto> CreateAsync(int requestId, int technicianId, CreateOfferDto dto)
    {
        var request = await db.SupportRequests.FindAsync(requestId)
            ?? throw AppException.NotFound("Заявката не съществува.");

        if (!await requests.IsInTechnicianDeviceTypesAsync(technicianId, request.DeviceTypeId))
        {
            throw AppException.Forbidden("Заявката не е за вид устройство, с който работите.");
        }

        if (request.Status != RequestStatus.Open)
        {
            throw AppException.Conflict("Оферта се дава само по отворена заявка.");
        }

        if (await db.Offers.AnyAsync(o => o.SupportRequestId == requestId && o.TechnicianId == technicianId))
        {
            throw AppException.Conflict("Вече сте дали оферта по тази заявка.");
        }

        var technician = await db.Users.FindAsync(technicianId)
            ?? throw AppException.NotFound("Потребителят не съществува.");

        var offer = new Offer
        {
            SupportRequestId = requestId,
            TechnicianId = technicianId,
            Price = dto.Price,
            Message = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim(),
            ProposedDate = dto.ProposedDate,
            Status = OfferStatus.Pending
        };

        db.Offers.Add(offer);
        await db.SaveChangesAsync();

        return ToDto(offer, technician.FullName, technician.AvgRating);
    }

    /// <summary>Офертите по заявка: вижда ги собственикът и Admin.</summary>
    public async Task<List<OfferDto>> GetForRequestAsync(int requestId, int userId, string role)
    {
        var request = await db.SupportRequests.FindAsync(requestId)
            ?? throw AppException.NotFound("Заявката не съществува.");

        if (role != Roles.Admin && request.ClientId != userId)
        {
            throw AppException.Forbidden("Офертите вижда само собственикът на заявката.");
        }

        return await db.Offers
            .Where(o => o.SupportRequestId == requestId && o.Status != OfferStatus.Withdrawn)
            .OrderBy(o => o.Price)
            .Select(o => new OfferDto(
                o.OfferId,
                o.SupportRequestId,
                o.TechnicianId,
                o.Technician.FullName,
                o.Technician.AvgRating,
                o.Price,
                o.Message,
                o.ProposedDate,
                o.Status,
                o.CreatedAt))
            .ToListAsync();
    }

    /// <summary>„Моите оферти“ на техника, с текущия статус на заявката.</summary>
    public async Task<List<MyOfferDto>> GetMineAsync(int technicianId)
    {
        return await db.Offers
            .Where(o => o.TechnicianId == technicianId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new MyOfferDto(
                o.OfferId,
                o.SupportRequestId,
                o.SupportRequest.Title,
                o.SupportRequest.Status,
                o.Price,
                o.ProposedDate,
                o.Status,
                o.CreatedAt))
            .ToListAsync();
    }

    /// <summary>Оттегляне: само авторът, само докато офертата чака.</summary>
    public async Task WithdrawAsync(int offerId, int technicianId)
    {
        var offer = await db.Offers.FindAsync(offerId)
            ?? throw AppException.NotFound("Офертата не съществува.");

        if (offer.TechnicianId != technicianId)
        {
            throw AppException.Forbidden("Можете да оттеглите само своя оферта.");
        }

        if (offer.Status != OfferStatus.Pending)
        {
            throw AppException.Conflict("Може да се оттегли само чакаща оферта.");
        }

        offer.Status = OfferStatus.Withdrawn;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Приемане: цялата логика е в процедурата usp_AcceptOffer (заключване, проверки,
    /// три промени в една транзакция). Грешка 50001-50005 от нея става 409.
    /// ClientId идва от токена, така че чужд клиент не може да приеме оферта (IDOR).
    /// </summary>
    public async Task<AcceptOfferResultDto> AcceptAsync(int offerId, int clientId)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.usp_AcceptOffer @OfferId = {offerId}, @ClientId = {clientId}");

        var result = await db.Offers
            .Where(o => o.OfferId == offerId)
            .Select(o => new AcceptOfferResultDto(o.OfferId, o.SupportRequestId, o.SupportRequest.Status))
            .SingleAsync();

        return result;
    }

    public static OfferDto ToDto(Offer o, string technicianName, decimal? technicianRating) => new(
        o.OfferId,
        o.SupportRequestId,
        o.TechnicianId,
        technicianName,
        technicianRating,
        o.Price,
        o.Message,
        o.ProposedDate,
        o.Status,
        o.CreatedAt);
}
