using System.Diagnostics;
using System.IO;
using Conquerors.Commands;
using Conquerors.Data;
using Conquerors.Entities;
using Conquerors.Input;
using Conquerors.Persistence;
using Conquerors.Rendering;
using Conquerors.Systems;
using Conquerors.UI;
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
    public const ulong SandboxSeed = 0xC0117EE7_C0117EE7UL;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Pixel _pixel = null!;
    private GridRenderer _gridRenderer = null!;
    private BuildingRenderer _buildingRenderer = null!;
    private Hud _hud = null!;
    private SpriteFont _font = null!;

    private readonly InputManager _input = new();
    private readonly Camera2D _camera = new();
    private readonly CameraSystem _cameraSystem = new();
    private readonly ResourceSystem _resourceSystem = new();
    private readonly PlacementSystem _placementSystem = new(new[] { "collector", "barracks" });
    private readonly CommandBuffer _commands = new();
    private readonly CommandProcessor _commandProcessor;
    private readonly SimClock _simClock = new();
    private readonly FpsCounter _fpsCounter = new();
    private readonly Stopwatch _frameStopwatch = new();
    private readonly WorldSerializer _serializer = new();

    private World _world = null!;

    public GameRoot()
    {
        _commandProcessor = new CommandProcessor(_placementSystem);
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
        _font = Content.Load<SpriteFont>("Default");
        _hud = new Hud(_font, _pixel.Texture);

        string catalogPath = Path.Combine(System.AppContext.BaseDirectory, "assets", "data", "buildings.json");
        BuildingCatalog catalog = BuildingCatalog.LoadFromJson(catalogPath);

        Grid grid = new(GridWidthTiles, GridHeightTiles, TilePixels);
        _world = new World(grid, catalog, StartingCredits, SandboxSeed);
        TileCoord hqTile = new(GridWidthTiles / 2 - 1, GridHeightTiles / 2 - 1);
        _world.AddBuilding(new Building(_world.NextId(), "hq", hqTile));

        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
        _camera.Position = new Vector2(grid.PixelWidth * 0.5f, grid.PixelHeight * 0.5f);

        _frameStopwatch.Start();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Poll();

        if (_input.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        double dt = gameTime.ElapsedGameTime.TotalSeconds;

        // Per-frame: input, camera, command emission. These run at render rate so
        // the user sees no input lag and the camera stays smooth at high framerates.
        _cameraSystem.Update(_camera, _input, (float)dt, IsActive);
        _camera.ClampTo(new Rectangle(0, 0, _world.Grid.PixelWidth, _world.Grid.PixelHeight));
        UpdatePlacement();
        UpdatePersistence();

        // Per-tick: sim systems advance in fixed 50ms steps so determinism is
        // independent of the host's framerate. All world mutation happens here.
        int steps = _simClock.Advance(dt);
        for (int i = 0; i < steps; i++)
        {
            SimStep();
        }

        base.Update(gameTime);
    }

    private void SimStep()
    {
        _resourceSystem.Update(_world, SimClock.TickDt);
        _commandProcessor.ProcessAll(_world, _commands);
    }

    private void UpdatePersistence()
    {
        if (_input.WasKeyPressed(Keys.F5))
        {
            try { _serializer.Save(_world, _resourceSystem, SavePaths.DefaultSaveFile); }
            catch (System.Exception ex) { System.Console.Error.WriteLine($"save failed: {ex.Message}"); }
        }
        if (_input.WasKeyPressed(Keys.F9))
        {
            try
            {
                bool loaded = _serializer.Load(_world, _resourceSystem, SavePaths.DefaultSaveFile);
                if (!loaded) System.Console.Error.WriteLine($"no save at {SavePaths.DefaultSaveFile}");
                _placementSystem.ExitBuildMode();
            }
            catch (System.Exception ex) { System.Console.Error.WriteLine($"load failed: {ex.Message}"); }
        }
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
            if (_placementSystem.Check(_world, _placementSystem.SelectedDefinitionId, tile) == PlacementResult.Ok)
            {
                _commands.Enqueue(new PlaceBuildingCommand(PlayerId.Local, _placementSystem.SelectedDefinitionId, tile));
            }
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
        _fpsCounter.Tick(_frameStopwatch.Elapsed.TotalSeconds);
        _frameStopwatch.Restart();

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
            bool valid = _placementSystem.Check(_world, _placementSystem.SelectedDefinitionId, tile) == PlacementResult.Ok;
            _buildingRenderer.DrawGhost(_spriteBatch, _world, tile, _placementSystem.SelectedDefinitionId, valid);
        }
        _spriteBatch.End();

        _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);
        _hud.Draw(_spriteBatch, _world, _placementSystem, _cameraSystem, _fpsCounter.Fps,
            _camera.ViewportWidth, _camera.ViewportHeight);
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
