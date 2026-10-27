using System.ComponentModel.DataAnnotations;

namespace ITspec.Api.View.DTOs;

public record DeviceTypeDto(int DeviceTypeId, string Name, bool IsActive);

public class SaveDeviceTypeDto
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 60 символа.")]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
