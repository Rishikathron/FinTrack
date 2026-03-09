using FinTrack.AI.Extensions;
using FinTrack.Interfaces;
using FinTrack.Providers;
using FinTrack.Services;
using FinTrack.Storage;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Railway/Render port binding
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
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Forward headers from Render's reverse proxy (HTTPS termination)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Must be first — tells ASP.NET the original request was HTTPS
// so Swagger/OpenAPI generates https:// URLs, not http://
app.UseForwardedHeaders();
app.UseHttpsRedirection();

// Swagger
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "FinTrack API");
});

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();