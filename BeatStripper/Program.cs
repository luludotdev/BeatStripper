using System;
using System.IO;
using System.Linq;

namespace BeatStripper
{
    class Program
    {
        internal static string InstallDirectory;

        static void Main(string[] _)
        {
            Logger.Log("Resolving Beat Saber install directory");
            InstallDirectory = InstallDir.GetInstallDir();
            if (InstallDirectory == null)
            {
                throw new Exception();
            }

            string libsDir = Path.Combine(InstallDirectory, @"Libs");
            string managedDir = Path.Combine(InstallDirectory, @"Beat Saber_Data\Managed");

            Logger.Log("Resolving Beat Saber version");
            string version = VersionFinder.FindVersion(InstallDirectory);

            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "stripped", version);
            Logger.Log("Creating output directory");
            Directory.CreateDirectory(outDir);

            string[] whitelist = new string[]
            {
                "IPA.",
                "TextMeshPro",
                "UnityEngine.",
                "Assembly-CSharp",
            };

            foreach (string f in ResolveDLLs(managedDir, whitelist))
            {
                if (File.Exists(f) == false) continue;
                var file = new FileInfo(f);
                Logger.Log($"Stripping {file.Name}");

                var mod = ModuleProcessor.Load(file.FullName, libsDir, managedDir);
                mod.Virtualize();
                mod.Strip();

                string outFile = Path.Combine(outDir, file.Name);
                mod.Write(outFile);
            }
        }

        internal static string[] ResolveDLLs(string managedDir, string[] whitelist)
        {
            var files = Directory.GetFiles(managedDir).Where(path => {
                FileInfo info = new FileInfo(path);
                if (info.Extension != ".dll") return false;

                foreach (string substr in whitelist)
                {
                    if (info.Name.Contains(substr)) return true;
                }

                return false;
            });

            return files.ToArray();
        }
    }
}
