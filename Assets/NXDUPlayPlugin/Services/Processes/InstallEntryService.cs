#nullable enable
using Cysharp.Threading.Tasks;
using LibraryPlugin;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Debug = UnityEngine.Debug;

public class InstallEntryService
{
    private readonly GameDetectionService gameDetectionService;

    public InstallEntryService(GameDetectionService gameDetectionService)
    {
        this.gameDetectionService = gameDetectionService;
    }

    public GameActionResult InstallEntry(UPlayPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            Debug.Log(Uplay.ClientExecPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = Uplay.ClientExecPath,
                Arguments = $"\"uplay://install/{entry.EntryId}\"",
                WindowStyle = ProcessWindowStyle.Maximized,
                UseShellExecute = true
            });

            Uplay.BringUplayToFront();

            _ = UniTask.RunOnThreadPool(async () => { await MonitorGameInstallation(plugin, entry, cancellationToken); }, cancellationToken: cancellationToken);

            return GameActionResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return GameActionResult.Fail;
        }
    }

    private async UniTask MonitorGameInstallation(UPlayPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
    {
        LibraryEntry? game;
        while (TryGetGame(entry.EntryId, out game) == false)
        {
            await UniTask.Delay(1000);

            if (cancellationToken.IsCancellationRequested)
            {
                if (plugin.OnEntryInstallationCancelled != null)
                    await plugin.OnEntryInstallationCancelled(entry.EntryId, plugin);

                return;
            }
        }

        if (plugin.OnEntryInstallationComplete != null)
        {
            var path = game.Path;
            await plugin.OnEntryInstallationComplete(entry.EntryId, path, plugin);
        }
    }

    private bool TryGetGame(string entryId, [NotNullWhen(true)] out LibraryEntry? game)
    {
        game = gameDetectionService.GetInstalledGames()
            .FirstOrDefault(x => x.EntryId == entryId);

        if (game == null)
            return false;

        return true;
    }
}