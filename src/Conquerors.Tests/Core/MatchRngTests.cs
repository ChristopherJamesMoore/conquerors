using System.Collections.Generic;
using Conquerors.Core;

namespace Conquerors.Tests.Core;

public class MatchRngTests
{
    [Fact]
    public void Same_Seed_Produces_Same_Sequence()
    {
        MatchRng a = new(42);
        MatchRng b = new(42);
        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt64(), b.NextUInt64());
        }
    }

    [Fact]
    public void Different_Seeds_Diverge_Quickly()
    {
        MatchRng a = new(1);
        MatchRng b = new(2);
        HashSet<ulong> aDraws = new();
        for (int i = 0; i < 16; i++) aDraws.Add(a.NextUInt64());
        for (int i = 0; i < 16; i++)
        {
            Assert.DoesNotContain(b.NextUInt64(), aDraws);
        }
    }

    [Fact]
    public void Seed_Zero_Produces_NonDegenerate_Sequence()
    {
        MatchRng r = new(0);
        // Without the SplitMix64 finalizer, xorshift seeded with 0 would emit 0 forever.
        ulong first = r.NextUInt64();
        ulong second = r.NextUInt64();
        Assert.NotEqual(0UL, first);
        Assert.NotEqual(0UL, second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void State_Save_And_Restore_Resumes_Sequence()
    {
        MatchRng original = new(0xC0FFEE);
        for (int i = 0; i < 50; i++) original.NextUInt64();
        ulong snapshot = original.State;
        ulong expectedNext = original.NextUInt64();

        MatchRng resumed = new(0xC0FFEE);
        resumed.SetState(snapshot);
        Assert.Equal(expectedNext, resumed.NextUInt64());
    }

    [Fact]
    public void NextInt_Stays_In_Half_Open_Range()
    {
        MatchRng r = new(7);
        for (int i = 0; i < 10000; i++)
        {
            int v = r.NextInt(5, 10);
            Assert.InRange(v, 5, 9);
        }
    }

    [Fact]
    public void NextInt_Throws_On_Empty_Range()
    {
        MatchRng r = new(1);
        Assert.Throws<System.ArgumentException>(() => r.NextInt(5, 5));
        Assert.Throws<System.ArgumentException>(() => r.NextInt(5, 4));
    }

    [Fact]
    public void NextBool_Approximates_Even_Distribution()
    {
        MatchRng r = new(123);
        int trueCount = 0;
        const int iterations = 10000;
        for (int i = 0; i < iterations; i++)
        {
            if (r.NextBool()) trueCount++;
        }
        // Allow a generous 5% tolerance — this isn't a stats test, just a sanity check.
        Assert.InRange(trueCount, iterations * 45 / 100, iterations * 55 / 100);
    }

    [Fact]
    public void Seed_Property_Holds_Original_Value_Across_Draws()
    {
        MatchRng r = new(0xABC123);
        for (int i = 0; i < 100; i++) r.NextUInt64();
        Assert.Equal(0xABC123UL, r.Seed);
    }
}
