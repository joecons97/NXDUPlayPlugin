using Cysharp.Threading.Tasks;
using LibraryPlugin;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class UPlayPlugin : LibraryPlugin.LibraryPlugin
{
    public override string Name => "Ubisoft Connect";

    public override string Description => "A library plugin to integrate with your Ubisoft Connect game library.";

    public override UniTask<ArtworkCollection> GetArtworkCollection(string entryId, CancellationToken cancellationToken)
    {
        var localCache = Uplay.GetLocalProductCache();
        var game = localCache.FirstOrDefault(x => x.uplay_id.ToString() == entryId);

        var result = new ArtworkCollection();

        if (game != null)
        {
            result.Banner = game.root?.logo_image;
            result.Cover = game.root?.thumb_image;
            result.Icon = game.root?.icon_image;
        }

        return UniTask.FromResult(result);
    }

    public override async UniTask<List<LibraryEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var localCache = Uplay.GetLocalProductCache();

        var entries = await localCache
            .Where(game => game?.root?.start_game?.offline?.executables != null)
            .Select(async game =>
        {
            var exe = game.root?.start_game?.offline?.executables?.FirstOrDefault();
            string path = ResolveExecutable(exe);

            return new LibraryEntry
            {
                EntryId = game.uplay_id.ToString(),
                Name = game.root.name,
                Path = path,
                IsInstalled = !string.IsNullOrEmpty(path)
            };
        });

        return entries.ToList();
    }

    private string ResolveExecutable(ProductInformation.Executable exe)
    {
        if (exe == null)
            return "";

        var key = @"SOFTWARE\";
        var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        var installsKey = root.OpenSubKey(key);
        if (installsKey == null)
        {
            root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            installsKey = root.OpenSubKey(key);
        }

        var localRegister = exe.working_directory?.register?.Replace("HKEY_LOCAL_MACHINE\\" + key, "") ?? "";

        var split = localRegister?.Split("\\") ?? new string[0];
        if (split.Length == 0)
            return "";

        var registryPath = string.Join("\\", split.Take(split.Length - 1));
        var valueName = split.Last();

        var gameData = installsKey.OpenSubKey(registryPath);
        return (gameData?.GetValue(valueName) as string)?.Replace('/', System.IO.Path.DirectorySeparatorChar);
    }
}
