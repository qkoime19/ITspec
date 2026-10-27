using System.Text;
using System.Text.Json.Serialization;
using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Infrastructure.OpenApi;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Локални настройки, които НЕ отиват в Git (виж .gitignore): тук е Jwt:Key на този компютър.
// Вместо файла може да се ползва и Manage User Secrets — и двете са извън Git.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// ---------- База данни (EF Core, Code First) ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---------- JWT ----------
// Ключът е в User Secrets (десен бутон върху проекта -> Manage User Secrets), не в appsettings.json.
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Липсва Jwt:Key или е под 32 символа. Десен бутон върху проекта ITspec.Api -> Manage User Secrets -> { \"Jwt:Key\": \"...\" }");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim-овете остават с кратките имена от токена (sub, name, role).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = AuthClaims.Name,
            RoleClaimType = AuthClaims.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();

// ---------- Услуги (бизнес логиката) ----------
builder.Services.AddScoped<AuthService>();

// ---------- Грешки: едно място, където изключение става HTTP отговор ----------
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ---------- Контролери и Swagger ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "ITspec API v1"));

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Прилага миграциите, които още не са в базата (същото като Update-Database).
    // Така при първото стартиране базата ITspec се създава сама.
    await db.Database.MigrateAsync();

    // Демо потребители и заявки само за разработка.
    var demoPassword = app.Configuration["Seed:DemoPassword"] ?? "Demo123!";
    await DbSeeder.SeedAsync(db, demoPassword);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
