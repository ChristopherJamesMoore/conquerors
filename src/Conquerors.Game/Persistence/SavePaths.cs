using System;
using System.IO;

namespace Conquerors.Persistence;

/// <summary>
/// Platform-appropriate paths for save files. macOS uses Application Support,
/// Windows uses AppData, Linux uses XDG_DATA_HOME (or ~/.local/share).
/// </summary>
public static class SavePaths
{
    public const string AppName = "Conquerors";
    public const string DefaultSaveFileName = "world.json";

    public static string GameDir
    {
        get
        {
            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", AppName);
            }
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppName);
            }
            // Linux / other
            string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            return Path.Combine(xdg, AppName);
        }
    }

    public static string DefaultSaveFile => Path.Combine(GameDir, DefaultSaveFileName);
}
