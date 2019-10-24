using System;
namespace BeatStripper
{
    class Program
    {
        internal static string InstallDirectory;

        static void Main(string[] args)
        {
            Logger.Log("Resolving Beat Saber install directory");
            InstallDirectory = InstallDir.GetInstallDir();
            if (InstallDirectory == null)
            {
                throw new Exception();
            }
        }
    }
}
