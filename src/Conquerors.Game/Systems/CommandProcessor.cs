using Conquerors.Commands;
using Conquerors.Core;

namespace Conquerors.Systems;

/// <summary>
/// Drains a <see cref="CommandBuffer"/> and dispatches each command to the system
/// that owns its application. The processor is the only thing that mutates world
/// state in response to player intent; renderers and HUDs never reach into systems
/// to do so.
/// </summary>
public sealed class CommandProcessor
{
    private readonly PlacementSystem _placement;

    public CommandProcessor(PlacementSystem placement)
    {
        _placement = placement;
    }

    /// <summary>
    /// Apply every command in <paramref name="buffer"/> in FIFO order, then clear it.
    /// Failed commands are silently dropped — the input layer is responsible for not
    /// emitting commands the player cannot afford or geometrically place.
    /// </summary>
    public void ProcessAll(World world, CommandBuffer buffer)
    {
        foreach (Command command in buffer.Pending)
        {
            Apply(world, command);
        }
        buffer.Clear();
    }

    private void Apply(World world, Command command)
    {
        switch (command)
        {
            case PlaceBuildingCommand place:
                _placement.Apply(world, place);
                break;
        }
    }
}
