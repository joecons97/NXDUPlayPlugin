using Microsoft.Win32;
using System.Linq;

public static class ProductInformationExecutableExtensions
{
    public static string ResolveExecutableLocation(this ProductInformation.Executable exe)
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
