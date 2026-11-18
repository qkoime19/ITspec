namespace ITspec.Api.Model.Entities;

/// <summary>
/// Потребител на системата. Клиент, техник и админ са една и съща
/// същност с различна роля, затова са в една таблица.
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Само хеш, никога паролата в чист вид.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Скрити данни: вижда ги само техникът с приетата оферта.</summary>
    public string? Phone { get; set; }

    /// <summary>Скрити данни: точният адрес на клиента, вижда го само техникът с приетата оферта.</summary>
    public string? Address { get; set; }

    /// <summary>Градът е публичен: по него техникът преценява дали да даде оферта.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Client, Technician или Admin (виж Roles).</summary>
    public string Role { get; set; } = Roles.Client;

    /// <summary>Потребителите не се трият, а се блокират.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>
    /// Среден рейтинг на техника. Съзнателна денормализация:
    /// поддържа се от usp_AddReview в транзакция.
    /// </summary>
    public decimal? AvgRating { get; set; }

    public int ReviewsCount { get; set; }

    public DateTime CreatedAt { get; set; }

    // --- Навигации ---

    /// <summary>Видовете устройства, с които работи техникът (връзка N:M).</summary>
    public ICollection<TechnicianDeviceType> TechnicianDeviceTypes { get; set; } = new List<TechnicianDeviceType>();

    /// <summary>Заявките, публикувани от клиента.</summary>
    public ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();

    /// <summary>Офертите, изпратени от техника.</summary>
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
}
