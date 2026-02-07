using System;
using System.Collections.Generic;
using System.IO;
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
            var gamesLocation = SettingsFile?.misc?.game_installation_path;
            if (gamesLocation == null)
            {
                return string.Empty;
            }
            else
            {
                var rootDir = Path.GetDirectoryName(gamesLocation);
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

    public static List<ProductInformation> GetLocalProductCache()
    {
        var initErrorMessage = "Ubisoft Connect client was not initialized, please start the client at least once to generate user library data.";
        var products = new List<ProductInformation>();
        var cachePath = ConfigurationsCachePath;
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

                        root.background_image = AssertUrlBase + root.background_image;
                    }

                    if (!string.IsNullOrEmpty(root.thumb_image))
                    {
                        if (loc.TryGetValue(root.thumb_image, out var locValue))
                        {
                            root.thumb_image = locValue;
                        }

                        root.thumb_image = AssertUrlBase + root.thumb_image;
                    }

                    if (!string.IsNullOrEmpty(root.logo_image))
                    {
                        if (loc.TryGetValue(root.logo_image, out var locValue))
                        {
                            root.logo_image = locValue;
                        }

                        root.logo_image = AssertUrlBase + root.logo_image;
                    }

                    if (!string.IsNullOrEmpty(root.dialog_image))
                    {
                        if (loc.TryGetValue(root.dialog_image, out var locValue))
                        {
                            root.dialog_image = locValue;
                        }

                        root.dialog_image = AssertUrlBase + root.dialog_image;
                    }

                    if (!string.IsNullOrEmpty(root.icon_image))
                    {
                        if (loc.TryGetValue(root.icon_image, out var locValue))
                        {
                            root.icon_image = locValue;
                        }

                        root.icon_image = AssertUrlBase + root.icon_image;
                    }

                    productInfo.uplay_id = item.UplayId;
                    productInfo.install_id = item.InstallId;
                    products.Add(productInfo);
                }
            }
        }

        return products;
    }
}
