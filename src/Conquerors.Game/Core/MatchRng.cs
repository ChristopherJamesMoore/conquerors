using System;

namespace Conquerors.Core;

/// <summary>
/// Deterministic per-match PRNG. xorshift64* with a SplitMix64 seed finalizer —
/// small, well-known, and stable across .NET versions and CPU architectures.
/// All gameplay-side randomness MUST go through one of these; never call
/// <see cref="Random.Shared"/> or any unseeded source from sim code.
///
/// The match seed is captured at construction; the current internal state is
/// exposed via <see cref="State"/> / <see cref="SetState"/> so saves can resume
/// mid-sequence.
/// </summary>
public sealed class MatchRng
{
    private ulong _state;

    /// <summary>The seed the match was constructed with. Stored verbatim for audit/repro.</summary>
    public ulong Seed { get; }

    /// <summary>Current internal state. Persist to save files for resumeable RNG.</summary>
    public ulong State => _state;

    public MatchRng(ulong seed)
    {
        Seed = seed;
        _state = SeedToState(seed);
    }

    /// <summary>Replace the current state — used by the persistence layer to resume.</summary>
    public void SetState(ulong state)
    {
        _state = state == 0 ? SeedToState(0) : state;
    }

    /// <summary>Raw 64-bit draw. All other Next* helpers route through this.</summary>
    public ulong NextUInt64()
    {
        ulong x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * 0x2545F4914F6CDD1DUL;
    }

    /// <summary>Uniform-ish integer in <c>[minInclusive, maxExclusive)</c>. Modulo bias is
    /// negligible at gameplay scales; use <see cref="NextUInt64"/> directly for cryptographic needs (we have none).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException("max must be > min", nameof(maxExclusive));
        }
        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt64() % range);
    }

    public bool NextBool() => (NextUInt64() & 1UL) != 0UL;

    private static ulong SeedToState(ulong seed)
    {
        // SplitMix64 finalizer — turns any seed (including 0) into a well-distributed
        // non-zero state suitable for xorshift64*.
        ulong z = seed + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return z == 0 ? 0x9E3779B97F4A7C15UL : z;
    }
}
