using ITspec.Api.Model.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Data;

/// <summary>
/// Демо клиенти, техници и две заявки — само в Development, за да има с какво да се пробва.
/// Администраторът и видовете устройства НЕ са тук: те идват от миграцията (HasData).
/// Паролата на демо потребителите е в appsettings.Development.json (Seed:DemoPassword).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, string demoPassword)
    {
        var hasher = new PasswordHasher<User>();

        // (име, имейл, телефон, адрес, град, роля, видове устройства)
        var demoUsers = new (string Name, string Email, string Phone, string? Address, string City, string Role, int[] DeviceTypeIds)[]
        {
            ("Мария Иванова", "maria@itspec.local", "0888111111", "ул. Христо Ботев 10, ет. 3", "Враца", Roles.Client, Array.Empty<int>()),
            ("Петър Петров", "petar@itspec.local", "0888222222", "бул. Демокрация 5", "Враца", Roles.Client, Array.Empty<int>()),
            ("Иван Техников", "ivan@itspec.local", "0888333333", null, "Враца", Roles.Technician, new[] { 1, 2 }),
            ("Георги Мобилов", "georgi@itspec.local", "0888444444", null, "Враца", Roles.Technician, new[] { 3, 4 }),
            ("Никола Сервизов", "nikola@itspec.local", "0888555555", null, "Монтана", Roles.Technician, new[] { 1, 3, 5, 6 }),
        };

        foreach (var d in demoUsers)
        {
            if (await db.Users.AnyAsync(u => u.Email == d.Email))
            {
                continue;
            }

            var user = new User
            {
                FullName = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Address = d.Address,
                City = d.City,
                Role = d.Role
            };
            user.PasswordHash = hasher.HashPassword(user, demoPassword);

            foreach (var typeId in d.DeviceTypeIds)
            {
                user.TechnicianDeviceTypes.Add(new TechnicianDeviceType { DeviceTypeId = typeId });
            }

            db.Users.Add(user);
        }

        await db.SaveChangesAsync();

        if (!await db.SupportRequests.AnyAsync())
        {
            var maria = await db.Users.SingleAsync(u => u.Email == "maria@itspec.local");

            db.SupportRequests.AddRange(
                new SupportRequest
                {
                    ClientId = maria.UserId,
                    DeviceTypeId = 1,
                    Title = "Лаптопът загрява и се изключва",
                    Description = "След около 20 минути работа вентилаторът е много шумен и лаптопът се изключва сам.",
                    DeviceModel = "Lenovo IdeaPad 5",
                    City = maria.City
                },
                new SupportRequest
                {
                    ClientId = maria.UserId,
                    DeviceTypeId = 3,
                    Title = "Счупен екран на телефон",
                    Description = "Екранът е напукан, но телефонът работи. Трябва смяна на дисплея.",
                    DeviceModel = "Samsung Galaxy A54",
                    City = maria.City
                });

            await db.SaveChangesAsync();
        }
    }
}
