using Conquerors.Input;
using Conquerors.Rendering;
using Conquerors.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Conquerors.Core;

/// <summary>
/// Top-level MonoGame game class. Owns graphics, input, the world, and per-frame
/// update/draw orchestration. Holds no gameplay logic itself; delegates to systems.
/// </summary>
public sealed class GameRoot : Game
{
    public const int GridWidthTiles = 64;
    public const int GridHeightTiles = 64;
    public const int TilePixels = 32;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Pixel _pixel = null!;
    private GridRenderer _gridRenderer = null!;

    private readonly InputManager _input = new();
    private readonly Camera2D _camera = new();
    private readonly CameraSystem _cameraSystem = new();
    private readonly Grid _grid = new(GridWidthTiles, GridHeightTiles, TilePixels);

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
        _pixel = new Pixel(GraphicsDevice);
        _gridRenderer = new GridRenderer(_pixel.Texture);

        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
        _camera.Position = new Vector2(_grid.PixelWidth * 0.5f, _grid.PixelHeight * 0.5f);
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
        _camera.ClampTo(new Rectangle(0, 0, _grid.PixelWidth, _grid.PixelHeight));

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 18, 24));

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetViewMatrix());
        _gridRenderer.Draw(_spriteBatch, _grid, _camera);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pixel?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
