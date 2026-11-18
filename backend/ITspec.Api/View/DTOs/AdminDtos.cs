namespace ITspec.Api.View.DTOs;

public record AdminUserDto(int UserId, string FullName, string Email, string City, string Role, bool IsBlocked, DateTime CreatedAt);

public record StatsDto(
    int Clients,
    int Technicians,
    int OpenRequests,
    int AssignedRequests,
    int CompletedRequests,
    int CancelledRequests,
    int Offers,
    int Reviews);
