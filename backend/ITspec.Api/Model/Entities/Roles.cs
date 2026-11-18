namespace ITspec.Api.Model.Entities;

/// <summary>
/// Трите роли в системата. По заданието ролята е колона Role в таблицата Users
/// (отделна таблица Roles не е нужна). Допустимите стойности се пазят с CHECK
/// ограничение в AppDbContext.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Client = "Client";
    public const string Technician = "Technician";
}
