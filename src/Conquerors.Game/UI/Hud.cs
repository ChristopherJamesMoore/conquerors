using Conquerors.Core;
using Conquerors.Data;
using Conquerors.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conquerors.UI;

/// <summary>
/// Screen-space HUD: credits, FPS, edge-scroll toggle, build mode, selected building info,
/// and a build-mode hint strip. Reads from systems and the world; does not mutate them.
/// </summary>
public sealed class Hud
{
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;

    public Hud(SpriteFont font, Texture2D pixel)
    {
        _font = font;
        _pixel = pixel;
    }

    public void Draw(
        SpriteBatch sb,
        World world,
        PlacementSystem placement,
        CameraSystem cameraSystem,
        int fps,
        int viewportWidth,
        int viewportHeight)
    {
        const int panelW = 340;
        int panelH = placement.BuildMode ? 116 : 96;
        sb.Draw(_pixel, new Rectangle(0, 0, panelW, panelH), new Color(0, 0, 0, 180));

        DrawLine(sb, $"Credits: {world.Credits}", 12, 8, Color.White);
        DrawLine(sb, $"FPS: {fps}", 12, 28, new Color(200, 200, 200));
        DrawLine(sb, $"Edge scroll: {(cameraSystem.EdgeScrollEnabled ? "ON" : "OFF")} (F2)",
            12, 48, new Color(200, 200, 200));
        Color modeColor = placement.BuildMode ? new Color(255, 220, 90) : new Color(180, 180, 180);
        DrawLine(sb, $"Mode: {(placement.BuildMode ? "BUILD" : "Idle")} (B)", 12, 68, modeColor);

        if (placement.BuildMode)
        {
            BuildingData def = world.Catalog.Get(placement.SelectedDefinitionId);
            string line = $"Selected: {def.Name}  cost {def.Cost}c  +{def.CreditsPerSecond:0.#}/s";
            DrawLine(sb, line, 12, 88, Color.White);
            DrawBuildHint(sb, viewportWidth, viewportHeight);
        }
    }

    private void DrawBuildHint(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        const string hint = "[1] Collector   [2] Barracks   LMB place   RMB cancel   B exit";
        Vector2 size = _font.MeasureString(hint);
        Vector2 pos = new(viewportWidth * 0.5f - size.X * 0.5f, viewportHeight - size.Y - 16);
        sb.Draw(_pixel,
            new Rectangle((int)pos.X - 12, (int)pos.Y - 6, (int)size.X + 24, (int)size.Y + 12),
            new Color(0, 0, 0, 180));
        sb.DrawString(_font, hint, pos, Color.White);
    }

    private void DrawLine(SpriteBatch sb, string text, int x, int y, Color color) =>
        sb.DrawString(_font, text, new Vector2(x, y), color);
}
