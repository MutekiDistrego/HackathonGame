using Microsoft.EntityFrameworkCore;
using HackathonGame.SessionService.Data;
using HackathonGame.SessionService.Hubs;
using HackathonGame.SessionService.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<SessionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR
builder.Services.AddSignalR();

// Background timer service
builder.Services.AddHostedService<TimerBackgroundService>();

// ── НОВЕ: HTTP-клієнт до ML-сервісу ─────────────────────────
builder.Services.AddHttpClient("ml", c =>
{
    c.BaseAddress = new Uri(
        builder.Configuration["MlService:BaseUrl"] ?? "http://localhost:8084/");
    c.Timeout = TimeSpan.FromSeconds(3); // не блокуємо основний flow
});
builder.Services.AddScoped<MlRecommendationService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Session Service API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3001", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.MapControllers();
app.MapHub<SessionHub>("/hubs/session");

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
    db.Database.Migrate();
}

app.Run();