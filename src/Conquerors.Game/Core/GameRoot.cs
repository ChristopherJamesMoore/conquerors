using System.IO;
using Conquerors.Data;
using Conquerors.Entities;
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
    public const int StartingCredits = 500;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Pixel _pixel = null!;
    private GridRenderer _gridRenderer = null!;
    private BuildingRenderer _buildingRenderer = null!;

    private readonly InputManager _input = new();
    private readonly Camera2D _camera = new();
    private readonly CameraSystem _cameraSystem = new();
    private readonly ResourceSystem _resourceSystem = new();
    private readonly PlacementSystem _placementSystem = new(new[] { "collector", "barracks" });

    private World _world = null!;

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
        _buildingRenderer = new BuildingRenderer(_pixel.Texture);

        string catalogPath = Path.Combine(System.AppContext.BaseDirectory, "assets", "data", "buildings.json");
        BuildingCatalog catalog = BuildingCatalog.LoadFromJson(catalogPath);

        Grid grid = new(GridWidthTiles, GridHeightTiles, TilePixels);
        _world = new World(grid, catalog, StartingCredits);

        // Initial HQ centred on the grid
        TileCoord hqTile = new(GridWidthTiles / 2 - 1, GridHeightTiles / 2 - 1);
        _world.AddBuilding(new Building(_world.NextId(), "hq", hqTile));

        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
        _camera.Position = new Vector2(grid.PixelWidth * 0.5f, grid.PixelHeight * 0.5f);
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
        _camera.ClampTo(new Rectangle(0, 0, _world.Grid.PixelWidth, _world.Grid.PixelHeight));
        _resourceSystem.Update(_world, dt);
        UpdatePlacement();

        base.Update(gameTime);
    }

    private void UpdatePlacement()
    {
        if (_input.WasKeyPressed(Keys.B))
        {
            _placementSystem.ToggleBuildMode();
        }
        if (!_placementSystem.BuildMode)
        {
            return;
        }

        if (_input.WasKeyPressed(Keys.D1)) _placementSystem.SelectByIndex(0);
        if (_input.WasKeyPressed(Keys.D2)) _placementSystem.SelectByIndex(1);

        if (_input.RightClicked)
        {
            _placementSystem.ExitBuildMode();
            return;
        }

        if (_input.LeftClicked)
        {
            TileCoord tile = MouseTile();
            _placementSystem.TryPlace(_world, tile, out _);
        }
    }

    private TileCoord MouseTile()
    {
        Vector2 screen = new(_input.MousePosition.X, _input.MousePosition.Y);
        Vector2 world = _camera.ScreenToWorld(screen);
        return _world.Grid.WorldToTile(world);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 18, 24));

        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.GetViewMatrix());
        _gridRenderer.Draw(_spriteBatch, _world.Grid, _camera);
        _buildingRenderer.Draw(_spriteBatch, _world);
        if (_placementSystem.BuildMode)
        {
            TileCoord tile = MouseTile();
            bool valid = _placementSystem.Check(_world, tile) == PlacementResult.Ok;
            _buildingRenderer.DrawGhost(_spriteBatch, _world, tile, _placementSystem.SelectedDefinitionId, valid);
        }
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
