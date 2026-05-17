using System.Collections.Generic;

namespace Conquerors.Commands;

/// <summary>
/// Per-tick FIFO of pending commands. Input layer enqueues; the command processor
/// drains. Kept deliberately simple — in Phase 4 this becomes the per-tick bundle
/// boundary, but the shape (ordered list of commands) is the same.
/// </summary>
public sealed class CommandBuffer
{
    private readonly List<Command> _pending = new();

    public IReadOnlyList<Command> Pending => _pending;

    public int Count => _pending.Count;

    public void Enqueue(Command command) => _pending.Add(command);

    public void Clear() => _pending.Clear();
}
