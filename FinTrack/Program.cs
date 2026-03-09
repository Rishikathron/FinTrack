using FinTrack.AI.Extensions;
using FinTrack.Interfaces;
using FinTrack.Providers;
using FinTrack.Services;
using FinTrack.Storage;

var builder = WebApplication.CreateBuilder(args);

// Railway port binding
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Caching
builder.Services.AddMemoryCache();

// Storage
builder.Services.AddSingleton<JsonFileRepository>();

// Services
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IValuationService, ValuationService>();

// Price provider
builder.Services.AddHttpClient<IPriceProvider, MetalPriceProvider>();

// Semantic Kernel AI
builder.Services.AddFinTrackAI();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var extraOrigins = builder.Configuration["CORS:Origins"]?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        var origins = new List<string>
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost",
            "http://localhost:80"
        };

        origins.AddRange(extraOrigins);

        policy.WithOrigins([.. origins])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Swagger
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "FinTrack API");
});

app.UseCors("AllowAngular");

app.UseAuthorization();
app.MapControllers();

app.Run();