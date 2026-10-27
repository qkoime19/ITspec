using ITspec.Api.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Data;

/// <summary>
/// Връзката на приложението с базата ITspec. Таблиците НЕ се пишат в SSMS:
/// EF Core ги създава от тези класове с миграцията InitialCreate (Code First).
/// Имената на DbSet-овете стават имена на таблиците.
/// Всяко ограничение тук отговаря на правило от docs/03-rules.md;
/// същите правила проверява Database/05_tests.sql.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Хешът на паролата „Admin123!“, изчислен веднъж с PasswordHasher и записан като константа.
    /// Ако се изчислява при всяко стартиране, хешът е различен (нова сол) и EF би правил нова миграция всеки път.
    /// </summary>
    public const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg97z2IZyhqC3YaJEn8frGtw7fMb52Ky5VJvyBDCaUXU9w==";

    public DbSet<User> Users => Set<User>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<TechnicianDeviceType> TechnicianDeviceTypes => Set<TechnicianDeviceType>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureUsers(modelBuilder);
        ConfigureDeviceTypes(modelBuilder);
        ConfigureTechnicianDeviceTypes(modelBuilder);
        ConfigureSupportRequests(modelBuilder);
        ConfigureOffers(modelBuilder);
        ConfigureReviews(modelBuilder);
        SeedData(modelBuilder);
    }

    // ------------------------------------------------------------------
    // Users
    // ------------------------------------------------------------------
    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.UserId);

            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.Address).HasMaxLength(200);
            e.Property(u => u.City).IsRequired().HasMaxLength(60);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);

            e.Property(u => u.IsBlocked).HasDefaultValue(false);
            e.Property(u => u.AvgRating).HasPrecision(3, 2);
            e.Property(u => u.ReviewsCount).HasDefaultValue(0);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // Правило: имейлът е уникален (тест T1)
            e.HasIndex(u => u.Email).IsUnique();

            e.ToTable(t =>
            {
                // Правило: ролята е една от трите. Вместо таблица Roles — CHECK ограничение.
                t.HasCheckConstraint("CK_Users_Role", "[Role] IN (N'Admin', N'Client', N'Technician')");

                // Рейтингът е между 1 и 5, ако го има
                t.HasCheckConstraint("CK_Users_AvgRating", "[AvgRating] IS NULL OR [AvgRating] BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Users_ReviewsCount", "[ReviewsCount] >= 0");
            });
        });
    }

    // ------------------------------------------------------------------
    // DeviceTypes — категориите (видове устройства)
    // ------------------------------------------------------------------
    private static void ConfigureDeviceTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>(e =>
        {
            e.HasKey(d => d.DeviceTypeId);

            e.Property(d => d.Name).IsRequired().HasMaxLength(60);

            // Правило: няма два вида устройства с едно име
            e.HasIndex(d => d.Name).IsUnique();
        });
    }

    // ------------------------------------------------------------------
    // TechnicianDeviceTypes — връзка N:M
    // ------------------------------------------------------------------
    private static void ConfigureTechnicianDeviceTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TechnicianDeviceType>(e =>
        {
            // Правило: един техник не може да има един вид устройство два пъти
            e.HasKey(td => new { td.TechnicianId, td.DeviceTypeId });

            e.HasOne(td => td.Technician)
                .WithMany(u => u.TechnicianDeviceTypes)
                .HasForeignKey(td => td.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(td => td.DeviceType)
                .WithMany(d => d.TechnicianDeviceTypes)
                .HasForeignKey(td => td.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ------------------------------------------------------------------
    // SupportRequests
    // ------------------------------------------------------------------
    private static void ConfigureSupportRequests(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SupportRequest>(e =>
        {
            e.HasKey(r => r.SupportRequestId);

            e.Property(r => r.Title).IsRequired().HasMaxLength(120);
            e.Property(r => r.Description).IsRequired().HasMaxLength(2000);
            e.Property(r => r.DeviceModel).HasMaxLength(100);
            e.Property(r => r.City).IsRequired().HasMaxLength(60);

            // Статусът се пази като текст ('Open', 'Assigned', ...),
            // за да го четат и съхранените процедури.
            e.Property(r => r.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(RequestStatus.Open);

            e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // Правило: заявката е винаги в точно един от четирите статуса
            e.ToTable(t => t.HasCheckConstraint(
                "CK_SupportRequests_Status",
                "[Status] IN (N'Open', N'Assigned', N'Completed', N'Cancelled')"));

            // Индекс за честата заявка „отворени заявки за вид устройство“
            e.HasIndex(r => new { r.DeviceTypeId, r.Status });

            // Връзка 1:N Клиент -> Заявки
            e.HasOne(r => r.Client)
                .WithMany(u => u.SupportRequests)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Връзка 1:N Вид устройство -> Заявки
            e.HasOne(r => r.DeviceType)
                .WithMany(d => d.SupportRequests)
                .HasForeignKey(r => r.DeviceTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ------------------------------------------------------------------
    // Offers
    // ------------------------------------------------------------------
    private static void ConfigureOffers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Offer>(e =>
        {
            e.HasKey(o => o.OfferId);

            e.Property(o => o.Price).HasPrecision(10, 2);
            e.Property(o => o.Message).HasMaxLength(1000);

            e.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(OfferStatus.Pending);

            e.Property(o => o.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Offers_Price", "[Price] > 0");
                t.HasCheckConstraint(
                    "CK_Offers_Status",
                    "[Status] IN (N'Pending', N'Accepted', N'Rejected', N'Withdrawn')");
            });

            // Правило: един техник дава една оферта на заявка (тест T3)
            e.HasIndex(o => new { o.SupportRequestId, o.TechnicianId }).IsUnique();

            // Правило: най-много една приета оферта на заявка (тест T7).
            // Филтриран уникален индекс — втора линия на защита след
            // процедурата usp_AcceptOffer.
            e.HasIndex(o => o.SupportRequestId, "UX_Offers_OneAccepted")
                .IsUnique()
                .HasFilter("[Status] = N'Accepted'");

            // Връзка 1:N Заявка -> Оферти
            e.HasOne(o => o.SupportRequest)
                .WithMany(r => r.Offers)
                .HasForeignKey(o => o.SupportRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            // Връзка 1:N Техник -> Оферти.
            // Без Restrict тук SQL Server отказва миграцията с грешка
            // „multiple cascade paths“: от Users до Offers има два пътя
            // (директно и през SupportRequests).
            e.HasOne(o => o.Technician)
                .WithMany(u => u.Offers)
                .HasForeignKey(o => o.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ------------------------------------------------------------------
    // Reviews — връзка 1:0..1 със заявката
    // ------------------------------------------------------------------
    private static void ConfigureReviews(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(r => r.ReviewId);

            e.Property(r => r.Comment).HasMaxLength(1000);
            e.Property(r => r.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            // Правило: оценката е от 1 до 5 (тест T2)
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));

            // Правило: един отзив на заявка. HasForeignKey<Review> при връзка
            // едно-към-едно създава уникален индекс по SupportRequestId.
            e.HasOne(r => r.SupportRequest)
                .WithOne(q => q.Review)
                .HasForeignKey<Review>(r => r.SupportRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ------------------------------------------------------------------
    // Начални данни: видовете устройства и администраторът.
    // По заданието администраторът се създава от миграцията — регистрация като Admin няма.
    // Демо клиентите и техниците се създават от DbSeeder само в Development.
    // ------------------------------------------------------------------
    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeviceType>().HasData(
            new DeviceType { DeviceTypeId = 1, Name = "Лаптоп", IsActive = true },
            new DeviceType { DeviceTypeId = 2, Name = "Настолен компютър", IsActive = true },
            new DeviceType { DeviceTypeId = 3, Name = "Смартфон", IsActive = true },
            new DeviceType { DeviceTypeId = 4, Name = "Таблет", IsActive = true },
            new DeviceType { DeviceTypeId = 5, Name = "Принтер", IsActive = true },
            new DeviceType { DeviceTypeId = 6, Name = "Рутер и мрежа", IsActive = true });

        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                Email = "admin@itspec.local",
                PasswordHash = AdminPasswordHash,
                FullName = "Администратор",
                City = "Враца",
                Role = Roles.Admin,
                IsBlocked = false,
                ReviewsCount = 0,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}
