using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Conquerors.Data;

/// <summary>
/// Read-only registry of building definitions keyed by id. Loaded from JSON.
/// </summary>
public sealed class BuildingCatalog
{
    private readonly Dictionary<string, BuildingData> _byId;

    public BuildingCatalog(IEnumerable<BuildingData> entries)
    {
        _byId = new Dictionary<string, BuildingData>();
        foreach (BuildingData e in entries)
        {
            if (_byId.ContainsKey(e.Id))
            {
                throw new System.InvalidOperationException($"duplicate building id '{e.Id}'");
            }
            _byId[e.Id] = e;
        }
    }

    public IReadOnlyDictionary<string, BuildingData> All => _byId;

    public BuildingData Get(string id) =>
        _byId.TryGetValue(id, out BuildingData? b)
            ? b
            : throw new KeyNotFoundException($"unknown building id '{id}'");

    public bool Contains(string id) => _byId.ContainsKey(id);

    public static BuildingCatalog LoadFromJson(string path)
    {
        string json = File.ReadAllText(path);
        List<BuildingData> entries = JsonSerializer.Deserialize<List<BuildingData>>(json, JsonOptions)
            ?? throw new InvalidDataException($"'{path}' deserialised to null");
        return new BuildingCatalog(entries);
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };
}
