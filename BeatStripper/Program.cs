using System;
using System.IO;
using System.Linq;

namespace BeatStripper
{
    class Program
    {
        internal static string InstallDirectory;

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && args[0] != null)
                {
                    InstallDirectory = Path.GetDirectoryName(args[0]);
                    if (File.Exists(Path.Combine(InstallDirectory, InstallDir.BeatSaberEXE)) == false)
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    Logger.Log("Resolving Beat Saber install directory");
                    InstallDirectory = InstallDir.GetInstallDir();
                    if (InstallDirectory == null)
                    {
                        throw new Exception();
                    }
                }

                if (BSIPA.EnsureExists(InstallDirectory) == false)
                {
                    Logger.Log("Installed BSIPA");
                }

                if (BSIPA.IsPatched(InstallDirectory) == false)
                {
                    Logger.Log("Patching game with BSIPA");
                    BSIPA.PatchDir(InstallDirectory);
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
                    "0Harmony",
                    "Newtonsoft.Json",
                    "Main",
                    "Cinemachine",
                    "Colors",
                    "Core.dll",
                    "DynamicBone",
                    "FinalIK",
                    "Oculus",
                    "Steam",
                    "HMLib",
                    "HMRendering",
                    "HMUI",
                    "Rendering",
                    "VRUI",
                    "Zenject"
                };

                string[] blacklist = new string[]
                {
                    "System.Core.dll"
                };

                foreach (string f in ResolveDLLs(managedDir, whitelist, blacklist))
                {
                    StripDLL(f, outDir, libsDir, managedDir);
                }

                foreach (string f in ResolveDLLs(libsDir, whitelist, blacklist))
                {
                    StripDLL(f, outDir, libsDir, managedDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        internal static string[] ResolveDLLs(string managedDir, string[] whitelist, string[] blacklist)
        {
            var files = Directory.GetFiles(managedDir).Where(path =>
            {
                FileInfo info = new FileInfo(path);
                if (info.Extension != ".dll") return false;

                foreach (string substr in whitelist)
                {
                    if (info.Name.Contains(substr) && !blacklist.Contains(info.Name)) return true;
                }

                return false;
            });

            return files.ToArray();
        }

        internal static void StripDLL(string f, string outDir, params string[] resolverDirs)
        {
            if (File.Exists(f) == false) return;
            var file = new FileInfo(f);
            Logger.Log($"Stripping {file.Name}");

            var mod = ModuleProcessor.Load(file.FullName, resolverDirs);
            mod.Virtualize();
            mod.Strip();

            string outFile = Path.Combine(outDir, file.Name);
            mod.Write(outFile);
        }
    }
}
