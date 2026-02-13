using Cysharp.Threading.Tasks;
using LibraryPlugin;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Debug = UnityEngine.Debug;

public class UninstallEntryService
{
    private readonly GameDetectionService gameDetectionService;

    public UninstallEntryService(GameDetectionService gameDetectionService)
    {
        this.gameDetectionService = gameDetectionService;
    }

    public GameActionResult UninstallEntry(UPlayPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Uplay.ClientExecPath,
                Arguments = $"\"uplay://uninstall/{entry.EntryId}\"",
                WindowStyle = ProcessWindowStyle.Maximized,
                UseShellExecute = true
            });

            Uplay.BringUplayToFront();

            _ = UniTask.RunOnThreadPool(async () => { await MonitorGameUninstallation(plugin, entry, cancellationToken); }, cancellationToken: cancellationToken);

            return GameActionResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return GameActionResult.Fail;
        }
    }

    private async UniTask MonitorGameUninstallation(UPlayPlugin plugin, LibraryEntry entry, CancellationToken cancellationToken)
    {
        while (gameDetectionService.TryGetGame(entry.EntryId, out LibraryEntry game))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (plugin.OnEntryUninstallationCancelled != null)
                    await plugin.OnEntryUninstallationCancelled(entry.EntryId, plugin);

                return;
            }

            await UniTask.Delay(1000);
        }

        if (plugin.OnEntryUninstallationComplete != null)
            await plugin.OnEntryUninstallationComplete(entry.EntryId, plugin);
    }
}