using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.Core;

/// <summary>
/// Top-level MonoGame game class. Owns the GraphicsDeviceManager, the world,
/// and the update/draw pump. Keeps no gameplay logic itself — delegates to systems.
/// </summary>
public sealed class GameRoot : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;

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
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Microsoft.Xna.Framework.Input.Keyboard.GetState()
            .IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
        {
            Exit();
        }
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
