using Cysharp.Threading.Tasks;
using LibraryPlugin;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class StartClientService
{
    public void StartClient(LibraryLocation location)
    {
        Debug.Log(Uplay.ClientExecPath);

        UniTask.Create(async () =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Uplay.ClientExecPath,
                WindowStyle = ProcessWindowStyle.Maximized,
                UseShellExecute = true
            });

            await UniTask.Delay(500);

            Uplay.BringUplayToFront();
        });

    }
}