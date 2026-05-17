using System.Collections.Generic;
using System.IO;
using Conquerors.Data;

namespace Conquerors.Tests.Data;

public class BuildingCatalogTests
{
    private static BuildingData B(string id) =>
        new(id, id, Cost: 0, Width: 1, Height: 1, CreditsPerSecond: 0f, new ColorRgb(0, 0, 0));

    [Fact]
    public void Get_ReturnsByid()
    {
        BuildingCatalog c = new(new[] { B("a"), B("b") });
        Assert.Equal("a", c.Get("a").Id);
        Assert.True(c.Contains("b"));
    }

    [Fact]
    public void DuplicateIds_Throw()
    {
        Assert.Throws<System.InvalidOperationException>(() =>
            new BuildingCatalog(new[] { B("a"), B("a") }));
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        BuildingCatalog c = new(new[] { B("a") });
        Assert.Throws<KeyNotFoundException>(() => c.Get("missing"));
    }

    [Fact]
    public void LoadFromJson_RoundTripsBasicShape()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, """
            [
              { "Id":"x", "Name":"X", "Cost":42, "Width":1, "Height":2,
                "CreditsPerSecond":3.5, "Color": { "R":1, "G":2, "B":3 } }
            ]
            """);
            BuildingCatalog c = BuildingCatalog.LoadFromJson(tmp);
            BuildingData x = c.Get("x");
            Assert.Equal(42, x.Cost);
            Assert.Equal(3.5f, x.CreditsPerSecond);
            Assert.Equal((byte)2, x.Color.G);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
