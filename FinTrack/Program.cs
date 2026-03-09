using FinTrack.AI.Extensions;
using FinTrack.Interfaces;
using FinTrack.Providers;
using FinTrack.Services;
using FinTrack.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Caching for metal prices
builder.Services.AddMemoryCache();

// Storage
builder.Services.AddSingleton<JsonFileRepository>();

// Services (clean interfaces for Phase 2 Semantic Kernel compatibility)
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IValuationService, ValuationService>();

// Price provider with HttpClient
builder.Services.AddHttpClient<IPriceProvider, MetalPriceProvider>();

// Semantic Kernel AI (ChatService + plugins)
builder.Services.AddFinTrackAI();

// CORS — allow Angular frontend from any environment
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        // Read additional origins from config/env: CORS__Origins = "https://example.com,https://other.com"
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "FinTrack API");
    });
}

app.UseCors("AllowAngular");

app.UseAuthorization();
app.MapControllers();
app.Run();
