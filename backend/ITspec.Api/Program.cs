using System.Text.Json.Serialization;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- База данни (EF Core, Code First) ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---------- Услуги ----------
// Сервизът още е списък в паметта; в следващия commit чете от базата.
builder.Services.AddSingleton<DeviceTypeService>();

// ---------- Контролери и Swagger ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "ITspec API v1"));
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
