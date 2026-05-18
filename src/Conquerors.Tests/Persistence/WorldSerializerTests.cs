using System.IO;
using Conquerors.Core;
using Conquerors.Entities;
using Conquerors.Persistence;
using Conquerors.Systems;

namespace Conquerors.Tests.Persistence;

public class WorldSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesState()
    {
        World w = TestWorlds.Fresh(credits: 777);
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(0, 0), PlayerId.Local));
        w.AddBuilding(new Building(w.NextId(), "collector", new TileCoord(5, 5), PlayerId.Local));
        ResourceSystem rs = new();
        rs.SetCarry(0.33);

        string path = Path.Combine(Path.GetTempPath(), $"conquerors-test-{System.Guid.NewGuid()}.json");
        try
        {
            new WorldSerializer().Save(w, rs, path);

            World w2 = TestWorlds.Fresh(credits: 0);
            ResourceSystem rs2 = new();
            bool loaded = new WorldSerializer().Load(w2, rs2, path);

            Assert.True(loaded);
            Assert.Equal(777, w2.Credits);
            Assert.InRange(rs2.Carry, 0.32, 0.34);
            Assert.Equal(2, w2.Buildings.Count);
            Assert.Contains(w2.Buildings, b => b.DefinitionId == "hq" && b.Tile == new TileCoord(0, 0));
            Assert.Contains(w2.Buildings, b => b.DefinitionId == "collector" && b.Tile == new TileCoord(5, 5));
            // grid occupancy restored:
            Assert.False(w2.Grid.CanPlace(new RectInt(0, 0, 3, 3)));
            Assert.False(w2.Grid.CanPlace(new RectInt(5, 5, 2, 2)));
            // next id continued past max:
            int next = w2.NextId();
            Assert.True(next > 2);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_PreservesPlayers_And_BuildingOwners()
    {
        World w = TestWorlds.Fresh(credits: 0);
        // TestWorlds.Fresh already added PlayerId.Local; add a second.
        PlayerId other = new(2);
        w.AddPlayer(new Conquerors.Core.Player(other, "Dummy", new Conquerors.Core.TeamId(1), new Conquerors.Data.ColorRgb(200, 60, 60)));
        w.AddBuilding(new Building(w.NextId(), "hq", new TileCoord(0, 0), PlayerId.Local));
        w.AddBuilding(new Building(w.NextId(), "collector", new TileCoord(5, 5), other));

        string path = Path.Combine(Path.GetTempPath(), $"conquerors-owner-{System.Guid.NewGuid()}.json");
        try
        {
            new WorldSerializer().Save(w, new ResourceSystem(), path);

            World w2 = TestWorlds.Fresh(credits: 0);
            new WorldSerializer().Load(w2, new ResourceSystem(), path);

            Assert.Equal(2, w2.Players.Count);
            Assert.NotNull(w2.FindPlayer(PlayerId.Local));
            Assert.Equal("Dummy", w2.FindPlayer(other)!.Name);
            Assert.Equal(PlayerId.Local, w2.Buildings[0].Owner);
            Assert.Equal(other, w2.Buildings[1].Owner);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_PreservesRngState()
    {
        World w = TestWorlds.Fresh(credits: 0, seed: 0x12345);
        // Advance the rng so State diverges from initial seed-state.
        for (int i = 0; i < 20; i++) w.Rng.NextUInt64();
        ulong stateAtSave = w.Rng.State;

        string path = Path.Combine(Path.GetTempPath(), $"conquerors-rng-{System.Guid.NewGuid()}.json");
        try
        {
            new WorldSerializer().Save(w, new ResourceSystem(), path);
            ulong expectedNext = w.Rng.NextUInt64();

            World w2 = TestWorlds.Fresh(credits: 0, seed: 0x12345);
            new WorldSerializer().Load(w2, new ResourceSystem(), path);

            Assert.Equal(stateAtSave, w2.Rng.State);
            Assert.Equal(expectedNext, w2.Rng.NextUInt64());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_PreRng_Save_Defaults_Seed_To_Fresh()
    {
        // Saves written before prereq #3 had no Rng fields. STJ defaults them to 0;
        // loading must not corrupt the world's existing Rng state.
        string path = Path.Combine(Path.GetTempPath(), $"prerng-{System.Guid.NewGuid()}.json");
        File.WriteAllText(path, """
        { "Version": 1, "Credits": 100, "ResourceCarry": 0, "NextEntityId": 1, "Buildings": [] }
        """);
        try
        {
            World w = TestWorlds.Fresh(credits: 0, seed: 99);
            ulong rngBefore = w.Rng.State;
            new WorldSerializer().Load(w, new ResourceSystem(), path);
            Assert.Equal(100, w.Credits);
            // No Rng state in the save → world's Rng state untouched.
            Assert.Equal(rngBefore, w.Rng.State);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_Nonexistent_ReturnsFalse()
    {
        World w = TestWorlds.Fresh();
        ResourceSystem rs = new();
        string path = Path.Combine(Path.GetTempPath(), $"nope-{System.Guid.NewGuid()}.json");
        Assert.False(new WorldSerializer().Load(w, rs, path));
    }

    [Fact]
    public void Load_WrongVersion_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), $"badver-{System.Guid.NewGuid()}.json");
        File.WriteAllText(path, """{ "Version": 99, "Credits": 0, "ResourceCarry": 0, "NextEntityId": 1, "Buildings": [] }""");
        try
        {
            World w = TestWorlds.Fresh();
            ResourceSystem rs = new();
            Assert.Throws<InvalidDataException>(() => new WorldSerializer().Load(w, rs, path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UnknownDefinitionId_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), $"unkdef-{System.Guid.NewGuid()}.json");
        File.WriteAllText(path, """
        { "Version": 1, "Credits": 0, "ResourceCarry": 0, "NextEntityId": 2,
          "Buildings": [ { "Id": 1, "DefinitionId": "mystery", "X": 0, "Y": 0 } ] }
        """);
        try
        {
            World w = TestWorlds.Fresh();
            ResourceSystem rs = new();
            Assert.Throws<InvalidDataException>(() => new WorldSerializer().Load(w, rs, path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
