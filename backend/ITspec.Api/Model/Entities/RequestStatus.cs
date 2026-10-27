namespace ITspec.Api.Model.Entities;

/// <summary>
/// Жизнен цикъл на заявка: Open -> Assigned -> Completed, или Open/Assigned -> Cancelled.
/// Записва се в базата като текст ('Open', 'Assigned', ...), за да се чете
/// лесно и от съхранените процедури.
/// Open е първата стойност (0), затова е и стойността по подразбиране.
/// </summary>
public enum RequestStatus
{
    Open,
    Assigned,
    Completed,
    Cancelled
}
