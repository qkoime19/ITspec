using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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
