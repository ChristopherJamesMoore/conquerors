using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Conquerors.Input;

/// <summary>
/// Polled each frame. Stores current and previous frame snapshots so callers can ask
/// "was this just pressed/released" without owning their own previous-state tracking.
/// </summary>
public sealed class InputManager
{
    public KeyboardState Keyboard { get; private set; }
    public KeyboardState PreviousKeyboard { get; private set; }
    public MouseState Mouse { get; private set; }
    public MouseState PreviousMouse { get; private set; }

    public void Poll()
    {
        PreviousKeyboard = Keyboard;
        Keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        PreviousMouse = Mouse;
        Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
    }

    public bool IsKeyDown(Keys k) => Keyboard.IsKeyDown(k);
    public bool WasKeyPressed(Keys k) => Keyboard.IsKeyDown(k) && !PreviousKeyboard.IsKeyDown(k);

    public bool LeftClicked =>
        Mouse.LeftButton == ButtonState.Pressed && PreviousMouse.LeftButton == ButtonState.Released;
    public bool RightClicked =>
        Mouse.RightButton == ButtonState.Pressed && PreviousMouse.RightButton == ButtonState.Released;
    public bool LeftReleased =>
        Mouse.LeftButton == ButtonState.Released && PreviousMouse.LeftButton == ButtonState.Pressed;
    public bool RightReleased =>
        Mouse.RightButton == ButtonState.Released && PreviousMouse.RightButton == ButtonState.Pressed;
    public bool LeftDown => Mouse.LeftButton == ButtonState.Pressed;
    public bool RightDown => Mouse.RightButton == ButtonState.Pressed;
    public int ScrollDelta => Mouse.ScrollWheelValue - PreviousMouse.ScrollWheelValue;
    public Point MousePosition => Mouse.Position;
    public Point MouseDelta => Mouse.Position - PreviousMouse.Position;
}
