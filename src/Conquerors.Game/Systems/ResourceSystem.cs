using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Entities;

namespace Conquerors.Systems;

/// <summary>
/// Sums each building's CreditsPerSecond and accumulates fractional credits across frames,
/// awarding whole credits as they cross integer thresholds.
/// </summary>
public sealed class ResourceSystem
{
    private double _carry;

    public double Carry => _carry;

    public void SetCarry(double value) => _carry = value;

    public void Update(World world, float dt)
    {
        double incomePerSec = 0;
        foreach (Building b in world.Buildings)
        {
            BuildingData def = world.Catalog.Get(b.DefinitionId);
            incomePerSec += def.CreditsPerSecond;
        }

        _carry += incomePerSec * dt;
        if (_carry >= 1.0)
        {
            int whole = (int)_carry;
            world.Credits += whole;
            _carry -= whole;
        }
    }

    public void Reset() => _carry = 0;
}
