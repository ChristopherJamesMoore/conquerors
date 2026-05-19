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
    private PrimitiveRenderer _primitives = null!;
    private GridRenderer3D _gridRenderer = null!;
    private BuildingRenderer3D _buildingRenderer = null!;
    private Hud _hud = null!;
    private SpriteFont _font = null!;

    private readonly InputManager _input = new();
    private readonly RtsCamera3D _camera = new();
    private readonly RtsCameraController _cameraController = new();
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
        _primitives = new PrimitiveRenderer(GraphicsDevice);
        _gridRenderer = new GridRenderer3D();
        _buildingRenderer = new BuildingRenderer3D();
        _font = Content.Load<SpriteFont>("Default");
        _hud = new Hud(_font, _pixel.Texture);

        string catalogPath = Path.Combine(System.AppContext.BaseDirectory, "assets", "data", "buildings.json");
        BuildingCatalog catalog = BuildingCatalog.LoadFromJson(catalogPath);

        Grid grid = new(GridWidthTiles, GridHeightTiles, TilePixels);
        _world = new World(grid, catalog, StartingCredits, SandboxSeed);
        _world.AddPlayer(new Player(PlayerId.Local, "Player 1", TeamId.Solo, new ColorRgb(80, 150, 240)));
        TileCoord hqTile = new(GridWidthTiles / 2 - 1, GridHeightTiles / 2 - 1);
        _world.AddBuilding(new Building(_world.NextId(), "hq", hqTile, PlayerId.Local));

        _camera.ViewportWidth = Window.ClientBounds.Width;
        _camera.ViewportHeight = Window.ClientBounds.Height;
        _camera.Target = new Vector3(grid.Width * 0.5f, 0f, grid.Height * 0.5f);

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
        _cameraController.Update(_camera, _input, (float)dt, IsActive);
        _camera.ClampTargetTo(0f, 0f, _world.Grid.Width, _world.Grid.Height);
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

        if (_input.LeftClicked && TryMouseTile(out TileCoord tile))
        {
            if (_placementSystem.Check(_world, _placementSystem.SelectedDefinitionId, tile) == PlacementResult.Ok)
            {
                _commands.Enqueue(new PlaceBuildingCommand(PlayerId.Local, _placementSystem.SelectedDefinitionId, tile));
            }
        }
    }

    private bool TryMouseTile(out TileCoord tile)
    {
        if (!_camera.ScreenToGround(_input.MousePosition, out Vector3 hit))
        {
            tile = default;
            return false;
        }
        int x = (int)System.Math.Floor(hit.X);
        int y = (int)System.Math.Floor(hit.Z);
        if (x < 0 || y < 0 || x >= _world.Grid.Width || y >= _world.Grid.Height)
        {
            tile = default;
            return false;
        }
        tile = new TileCoord(x, y);
        return true;
    }

    protected override void Draw(GameTime gameTime)
    {
        _fpsCounter.Tick(_frameStopwatch.Elapsed.TotalSeconds);
        _frameStopwatch.Restart();

        GraphicsDevice.Clear(new Color(15, 18, 24));
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        GraphicsDevice.BlendState = BlendState.NonPremultiplied;
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        _primitives.SetCamera(_camera.View, _camera.Projection);
        _gridRenderer.Draw(_primitives, _world.Grid);
        _buildingRenderer.Draw(_primitives, _world);
        if (_placementSystem.BuildMode && TryMouseTile(out TileCoord ghostTile))
        {
            bool valid = _placementSystem.Check(_world, _placementSystem.SelectedDefinitionId, ghostTile) == PlacementResult.Ok;
            _buildingRenderer.DrawGhost(_primitives, _world, ghostTile, _placementSystem.SelectedDefinitionId, valid);
        }

        _spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);
        _hud.Draw(_spriteBatch, _world, _placementSystem, _cameraController, _fpsCounter.Fps,
            _camera.ViewportWidth, _camera.ViewportHeight);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _primitives?.Dispose();
            _pixel?.Dispose();
            _spriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}
