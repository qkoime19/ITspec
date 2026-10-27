namespace ITspec.Api.Model.Entities;

/// <summary>
/// Отзив на клиента за завършена заявка.
/// Връзка 1:0..1 със заявката — най-много един отзив на заявка.
/// </summary>
public class Review
{
    public int ReviewId { get; set; }

    public int SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = null!;

    /// <summary>Оценка от 1 до 5 (tinyint в базата).</summary>
    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
