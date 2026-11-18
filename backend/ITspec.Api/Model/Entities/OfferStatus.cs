namespace ITspec.Api.Model.Entities;

/// <summary>
/// Състояние на оферта. Записва се в базата като текст.
/// Pending е първата стойност (0), затова е и стойността по подразбиране.
/// </summary>
public enum OfferStatus
{
    Pending,
    Accepted,
    Rejected,
    Withdrawn
}
