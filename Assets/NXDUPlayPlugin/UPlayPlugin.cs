using Cysharp.Threading.Tasks;
using LibraryPlugin;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class UPlayPlugin : LibraryPlugin.LibraryPlugin
{
    public override string Name => "Ubisoft Connect";

    public override string Description => "A library plugin to integrate with your Ubisoft Connect game library.";

    private readonly GameArtworkService gameArtworkService = new();
    private readonly GameDetectionService gameDetectionService = new();
    private readonly StartEntryService gameLaunchService = new();
    private readonly StartClientService startClientService = new();
    private readonly GameMetadataService gameMetadataService = new();
    private InstallEntryService installEntryService => new(gameDetectionService);
    private UninstallEntryService uninstallEntryService => new(gameDetectionService);

    public override async UniTask<AdditionalMetadata> GetAdditionalMetadata(string entryId, CancellationToken cancellationToken)
    {
        if (gameDetectionService.TryGetGame(entryId, out var game) == false || game.root == null)
            return null;

        return await gameMetadataService.GetGameMetadata(new LibraryEntry() { EntryId = entryId, Name = game.root.name });
    }

    public override async UniTask<ArtworkCollection> GetArtworkCollection(string entryId, CancellationToken cancellationToken)
    {
        var localCache = gameDetectionService.GetLocalProductCache();
        var game = localCache.FirstOrDefault(x => x.uplay_id.ToString() == entryId);
        var cover = await gameArtworkService.GetCoverUrlAsync(game.root.name);

        var result = new ArtworkCollection();

        if (game != null)
        {
            result.Cover = cover ?? game.root?.thumb_image;
            result.Banner = game.root?.logo_image;
            result.Icon = game.root?.icon_image;
        }

        return result;
    }

    public override async UniTask<List<LibraryEntry>> GetEntriesAsync(CancellationToken cancellationToken)
    {
        var localCache = gameDetectionService.GetLocalProductCache();

        var entries = await localCache
            .Where(game => game?.root?.start_game?.offline?.executables != null)
            .Select(async game =>
        {
            var exe = game.root?.start_game?.offline?.executables?.FirstOrDefault();
            var path = exe.ResolveExecutableLocation();

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

    public override UniTask<GameActionResult> TryStartEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
    {
        return UniTask.FromResult(gameLaunchService.StartEntry(this, entry, cancellationToken));
    }

    public override UniTask OpenLibraryApplication(LibraryLocation location)
    {
        startClientService.StartClient(location);

        return UniTask.CompletedTask;
    }

    public override UniTask<GameActionResult> TryInstallEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
    {
        var result = installEntryService.InstallEntry(this, entry, cancellationToken);
        return UniTask.FromResult(result);
    }

    public override UniTask<GameActionResult> TryUninstallEntryAsync(LibraryEntry entry, CancellationToken cancellationToken)
    {
        var result = uninstallEntryService.UninstallEntry(this, entry, cancellationToken);
        return UniTask.FromResult(result);
    }
}
