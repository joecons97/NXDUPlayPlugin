using LibraryPlugin;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

public class GameDetectionService
{
    public List<ProductInformation> GetLocalProductCache()
    {
        var initErrorMessage = "Ubisoft Connect client was not initialized, please start the client at least once to generate user library data.";
        var products = new List<ProductInformation>();
        var cachePath = Uplay.ConfigurationsCachePath;
        if (!File.Exists(cachePath))
        {
            throw new FileNotFoundException(initErrorMessage);
        }

        using (var file = File.OpenRead(cachePath))
        {
            var cacheData = ProtoBuf.Serializer.Deserialize<UplayCacheGameCollection>(file);

            if (cacheData?.Games.Count == 0)
            {
                throw new FileNotFoundException(initErrorMessage);
            }

            var yamlDeserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (var item in cacheData.Games)
            {
                if (!string.IsNullOrEmpty(item.GameInfo))
                {
                    var productInfo = yamlDeserializer.Deserialize<ProductInformation>(item.GameInfo);

                    var root = productInfo.root;
                    var loc = productInfo.localizations?.@default ?? new Dictionary<string, string>();
                    if (!string.IsNullOrEmpty(root.name))
                    {
                        if (loc.TryGetValue(root.name, out var locValue))
                        {
                            root.name = locValue;
                        }
                    }

                    if (!string.IsNullOrEmpty(root.background_image))
                    {
                        if (loc.TryGetValue(root.background_image, out var locValue))
                        {
                            root.background_image = locValue;
                        }

                        root.background_image = Uplay.AssertUrlBase + root.background_image;
                    }

                    if (!string.IsNullOrEmpty(root.thumb_image))
                    {
                        if (loc.TryGetValue(root.thumb_image, out var locValue))
                        {
                            root.thumb_image = locValue;
                        }

                        root.thumb_image = Uplay.AssertUrlBase + root.thumb_image;
                    }

                    if (!string.IsNullOrEmpty(root.logo_image))
                    {
                        if (loc.TryGetValue(root.logo_image, out var locValue))
                        {
                            root.logo_image = locValue;
                        }

                        root.logo_image = Uplay.AssertUrlBase + root.logo_image;
                    }

                    if (!string.IsNullOrEmpty(root.dialog_image))
                    {
                        if (loc.TryGetValue(root.dialog_image, out var locValue))
                        {
                            root.dialog_image = locValue;
                        }

                        root.dialog_image = Uplay.AssertUrlBase + root.dialog_image;
                    }

                    if (!string.IsNullOrEmpty(root.icon_image))
                    {
                        if (loc.TryGetValue(root.icon_image, out var locValue))
                        {
                            root.icon_image = locValue;
                        }

                        root.icon_image = Uplay.AssertUrlBase + root.icon_image;
                    }

                    productInfo.uplay_id = item.UplayId;
                    productInfo.install_id = item.InstallId;
                    products.Add(productInfo);
                }
            }
        }

        return products;
    }

    public bool TryGetGame(string entryId, [NotNullWhen(true)] out LibraryEntry? game)
    {
        game = GetInstalledGames()
            .FirstOrDefault(x => x.EntryId == entryId);

        if (game == null)
            return false;

        return true;
    }

    public List<LibraryEntry> GetInstalledGames()
    {
        var games = new List<LibraryEntry>();

        var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        var installsKey = root.OpenSubKey(@"SOFTWARE\ubisoft\Launcher\Installs\");
        if (installsKey == null)
        {
            root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            installsKey = root.OpenSubKey(@"SOFTWARE\ubisoft\Launcher\Installs\");
        }

        if (installsKey != null)
        {
            foreach (var install in installsKey.GetSubKeyNames())
            {
                using var gameData = installsKey.OpenSubKey(install);
                if (gameData == null)
                    continue;

                var installDir = (gameData.GetValue("InstallDir") as string)?.Replace('/', Path.DirectorySeparatorChar);
                if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
                    continue;

                var downloadPath = Path.Combine(installDir, "uplay_download");
                if (!Directory.Exists(downloadPath) || Directory.GetFileSystemEntries(downloadPath).Length == 0)
                {
                    var newGame = new LibraryEntry()
                    {
                        EntryId = install,
                        Path = installDir,
                        Name = Path.GetFileName(installDir.TrimEnd(Path.DirectorySeparatorChar)),
                        IsInstalled = true
                    };

                    games.Add(newGame);
                }
            }
        }

        installsKey.Dispose();
        root.Dispose();

        return games;
    }
}
