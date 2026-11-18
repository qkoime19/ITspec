namespace ITspec.Api.Model.Entities;


public class Review
{
    public int ReviewId { get; set; }

    public int SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = null!;

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
