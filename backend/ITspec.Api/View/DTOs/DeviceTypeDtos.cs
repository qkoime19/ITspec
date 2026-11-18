using System.ComponentModel.DataAnnotations;

namespace ITspec.Api.View.DTOs;

/// <summary>Вид устройство. RequestsCount е броят заявки към вида (навигация в Select -> една SQL заявка).</summary>
public record DeviceTypeDto(int DeviceTypeId, string Name, bool IsActive, int RequestsCount);

public class SaveDeviceTypeDto
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 60 символа.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
