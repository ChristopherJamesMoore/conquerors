using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Conquerors.Tests.Determinism;

/// <summary>
/// Sim-code source files must not use APIs that compromise lockstep determinism
/// (transcendentals, wall clocks, unseeded RNG, allocation-derived ids). This
/// scanner is the enforcement layer for the rules listed in
/// <c>docs/ARCHITECTURE-MP.md §Determinism discipline</c>. A Roslyn analyzer is
/// the longer-term home; line scanning is good enough until then.
/// </summary>
public class DeterminismDisciplineTests
{
    private static readonly string[] SimSubdirs =
    {
        "Commands",
        "Core",
        "Data",
        "Entities",
        "Systems",
    };

    /// <summary>Files inside scanned dirs that are exempt — these are wiring/
    /// presentation, not gameplay sim.</summary>
    private static readonly HashSet<string> ExemptFiles = new(StringComparer.Ordinal)
    {
        // GameRoot is the MonoGame wiring layer; uses Stopwatch for the FPS counter
        // and System.Diagnostics for frame timing. None of its state is sim state.
        "GameRoot.cs",
    };

    private static readonly (string Pattern, string Reason)[] Forbidden =
    {
        ("Math.Sin",       "transcendental — varies across CPUs / runtimes"),
        ("Math.Cos",       "transcendental"),
        ("Math.Tan",       "transcendental"),
        ("Math.Atan",      "transcendental"),
        ("Math.Atan2",     "transcendental"),
        ("Math.Asin",      "transcendental"),
        ("Math.Acos",      "transcendental"),
        ("Math.Exp",       "transcendental"),
        ("Math.Log",       "transcendental"),
        ("Math.Pow",       "transcendental"),
        ("MathF.",         "MathF transcendentals carry the same risk as Math.*"),
        ("Random.Shared",  "unseeded RNG — use World.Rng (MatchRng)"),
        ("new Random(",    "ad-hoc RNG — use World.Rng (MatchRng)"),
        ("DateTime.Now",   "wall clock — not deterministic across hosts"),
        ("DateTime.UtcNow","wall clock — not deterministic across hosts"),
        ("Stopwatch",      "wall clock — not deterministic in sim code"),
        ("Guid.NewGuid",   "allocation-derived id — not deterministic"),
    };

    [Fact]
    public void Sim_Code_Contains_No_Forbidden_APIs()
    {
        string simRoot = FindSimRoot();
        List<string> violations = new();

        foreach (string sub in SimSubdirs)
        {
            string dir = Path.Combine(simRoot, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (ExemptFiles.Contains(Path.GetFileName(file))) continue;
                string relative = Path.GetRelativePath(simRoot, file);
                foreach (string v in ScanLines(File.ReadLines(file)))
                {
                    violations.Add($"{relative}:{v}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            "Determinism violations:\n  " + string.Join("\n  ", violations));
    }

    [Fact]
    public void Scanner_Flags_Forbidden_Token()
    {
        string[] src =
        {
            "public double Wobble(double t) {",
            "    return Math.Sin(t);",
            "}",
        };
        List<string> v = ScanLines(src);
        Assert.Single(v);
        Assert.Contains("Math.Sin", v[0]);
    }

    [Fact]
    public void Scanner_Ignores_Forbidden_Token_In_Line_Comment()
    {
        string[] src = { "// Math.Sin is on the forbidden list because…" };
        Assert.Empty(ScanLines(src));
    }

    [Fact]
    public void Scanner_Ignores_Forbidden_Token_In_Block_Comment()
    {
        string[] src =
        {
            "/* example reason:",
            "   Math.Sin would break cross-platform replays",
            "*/",
            "int x = 1;",
        };
        Assert.Empty(ScanLines(src));
    }

    [Fact]
    public void Scanner_Ignores_Using_Directives()
    {
        // A `using static System.Math;` would otherwise look like a Math.* hit.
        string[] src = { "using static System.Math;" };
        Assert.Empty(ScanLines(src));
    }

    [Fact]
    public void Scanner_Reports_All_Hits_With_Line_Numbers()
    {
        string[] src =
        {
            "int a = 1;",
            "double y = Math.Cos(x);",
            "var r = new Random(42);",
            "int b = 2;",
        };
        List<string> v = ScanLines(src);
        Assert.Equal(2, v.Count);
        Assert.Contains("L2", v[0]);
        Assert.Contains("Math.Cos", v[0]);
        Assert.Contains("L3", v[1]);
        Assert.Contains("new Random(", v[1]);
    }

    internal static List<string> ScanLines(IEnumerable<string> lines)
    {
        List<string> violations = new();
        bool inBlockComment = false;
        int lineNo = 0;
        foreach (string raw in lines)
        {
            lineNo++;
            string code = StripComments(raw, ref inBlockComment);
            if (string.IsNullOrWhiteSpace(code)) continue;
            if (code.TrimStart().StartsWith("using ", StringComparison.Ordinal)) continue;
            foreach ((string pat, string reason) in Forbidden)
            {
                if (code.Contains(pat, StringComparison.Ordinal))
                {
                    violations.Add($"L{lineNo}: '{pat}' — {reason}");
                }
            }
        }
        return violations;
    }

    private static string StripComments(string line, ref bool inBlock)
    {
        // Light-weight: handles // and /* */. Doesn't unparse strings, which is
        // fine — the forbidden tokens don't naturally appear inside literals.
        StringBuilder sb = new(line.Length);
        for (int i = 0; i < line.Length; i++)
        {
            if (inBlock)
            {
                if (i + 1 < line.Length && line[i] == '*' && line[i + 1] == '/')
                {
                    inBlock = false;
                    i++;
                }
                continue;
            }
            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*')
            {
                inBlock = true;
                i++;
                continue;
            }
            if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
            {
                break;
            }
            sb.Append(line[i]);
        }
        return sb.ToString();
    }

    private static string FindSimRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Conquerors.sln")))
            {
                return Path.Combine(dir.FullName, "src", "Conquerors.Game");
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (Conquerors.sln above test bin)");
    }
}
