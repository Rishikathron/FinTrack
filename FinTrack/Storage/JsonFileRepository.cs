using System.Text.Json;
using FinTrack.Models;

namespace FinTrack.Storage;

/// <summary>
/// Simple JSON file-based storage. One file per user.
/// Thread-safe using a lock per user file.
/// </summary>
public class JsonFileRepository
{
    private readonly string _dataFolder;
    private static readonly object _lock = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonFileRepository(IWebHostEnvironment env)
    {
        _dataFolder = Path.Combine(env.ContentRootPath, "AppData");
        Directory.CreateDirectory(_dataFolder);
    }

    private string GetFilePath(string userId) => Path.Combine(_dataFolder, $"{userId}.json");

    public List<Asset> Load(string userId)
    {
        var path = GetFilePath(userId);
        lock (_lock)
        {
            if (!File.Exists(path)) return [];
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<Asset>>(json, _jsonOptions) ?? [];
        }
    }

    public void Save(string userId, List<Asset> assets)
    {
        var path = GetFilePath(userId);
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(assets, _jsonOptions);
            File.WriteAllText(path, json);
        }
    }
}
