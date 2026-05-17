using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Conquerors.Core;
using Conquerors.Entities;
using Conquerors.Systems;

namespace Conquerors.Persistence;

/// <summary>
/// Serialises a World + ResourceSystem to JSON and restores them. The BuildingCatalog
/// is not saved — definitions are loaded from assets/data on startup and referenced by id.
/// </summary>
public sealed class WorldSerializer
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public void Save(World world, ResourceSystem resources, string path)
    {
        List<BuildingSave> buildings = new(world.Buildings.Count);
        foreach (Building b in world.Buildings)
        {
            buildings.Add(new BuildingSave(b.Id, b.DefinitionId, b.Tile.X, b.Tile.Y));
        }
        SaveData data = new(
            CurrentVersion,
            world.Credits,
            resources.Carry,
            world.PeekNextId(),
            buildings,
            world.Rng.Seed,
            world.Rng.State);

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOpts));
    }

    /// <summary>Returns true if a save was found and applied, false if no save existed.</summary>
    public bool Load(World world, ResourceSystem resources, string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        string json = File.ReadAllText(path);
        SaveData data = JsonSerializer.Deserialize<SaveData>(json, JsonOpts)
            ?? throw new InvalidDataException("save file deserialised to null");
        if (data.Version != CurrentVersion)
        {
            throw new InvalidDataException(
                $"save version {data.Version} not supported (expected {CurrentVersion})");
        }

        world.Buildings.Clear();
        world.Grid.Clear();
        world.Credits = data.Credits;
        resources.SetCarry(data.ResourceCarry);
        // RngState is the resume-point; Seed is metadata. Old (pre-rng) saves omit
        // both fields → STJ defaults to 0 → MatchRng treats that as a fresh seed.
        if (data.RngState != 0)
        {
            world.Rng.SetState(data.RngState);
        }
        foreach (BuildingSave bs in data.Buildings)
        {
            if (!world.Catalog.Contains(bs.DefinitionId))
            {
                throw new InvalidDataException(
                    $"save references unknown building '{bs.DefinitionId}'");
            }
            world.AddBuilding(new Building(bs.Id, bs.DefinitionId, new TileCoord(bs.X, bs.Y)));
        }
        world.SetNextId(data.NextEntityId);
        return true;
    }
}
