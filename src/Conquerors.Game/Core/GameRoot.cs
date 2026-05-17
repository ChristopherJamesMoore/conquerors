using Conquerors.Input;
using Conquerors.Rendering;
using Conquerors.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Conquerors.Core;

/// <summary>
/// Top-level MonoGame game class. Owns graphics, input, the camera, and per-frame
/// update/draw orchestration. Holds no gameplay logic itself; delegates to systems.
/// </summary>
public sealed class GameRoot : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

    private readonly InputManager _input = new();
    private readonly Camera2D _camera = new();
    private readonly CameraSystem _cameraSystem = new();

    public GameRoot()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / 60.0);
        Window.AllowUserResizing = true;
        Window.Title = "Conquerors";
        Window.ClientSizeChanged += OnClientSizeChanged;
    }

    private void OnClientSizeChanged(object? sender, System.EventArgs e)
    {
        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Poll();

        if (_input.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cameraSystem.Update(_camera, _input, dt, IsActive);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 18, 24));
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
