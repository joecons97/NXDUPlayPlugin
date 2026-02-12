using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using YamlDotNet.Serialization;

public class Uplay
{
    [Serializable]
    public class UplaySettings
    {
        public MiscSettings misc { get; set; }
    }

    [Serializable]
    public class MiscSettings
    {
        public string game_installation_path { get; set; }
    }

    public const string AssertUrlBase = @"https://ubistatic3-a.akamaihd.net/orbit/uplay_launcher_3_0/assets/";

    public static string ClientExecPath
    {
        get
        {
            var path = InstallationPath;
            return string.IsNullOrEmpty(path) ? string.Empty : Path.Combine(path, "UbisoftConnect.exe");
        }
    }

    public static string InstallationPath
    {
        get
        {
            var gamesLocation = SettingsFile?.misc?.game_installation_path.TrimEnd('/');
            if (gamesLocation == null)
            {
                return string.Empty;
            }
            else
            {
                var rootDir = Directory.GetParent(gamesLocation).FullName;
                return rootDir;
            }
        }
    }

    public static string ConfigurationsCachePath
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Ubisoft Game Launcher",
                "cache",
                "configuration",
                "configurations");
        }
    }

    public static string SettingsPath
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Ubisoft Game Launcher",
                "settings.yaml");
        }
    }

    public static UplaySettings SettingsFile { get; } =
        new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<UplaySettings>(File.ReadAllText(SettingsPath));


    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;

    public static void BringUplayToFront()
    {
        // Try different possible process names
        string[] processNames = { "UbisoftConnect", "upc", "Uplay" };

        foreach (string processName in processNames)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    IntPtr handle = process.MainWindowHandle;

                    if (handle != IntPtr.Zero)
                    {
                        // If window is minimized, restore it
                        if (IsIconic(handle))
                        {
                            ShowWindow(handle, SW_RESTORE);
                        }

                        // Bring window to front
                        SetForegroundWindow(handle);

                        return;
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }
}
