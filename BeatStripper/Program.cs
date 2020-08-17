using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
                    if (Directory.Exists(args[0]))
                        InstallDirectory = args[0];
                    else
                        InstallDirectory = Path.GetDirectoryName(args[0]);
                    string beatSaberExe = Path.Combine(InstallDirectory, InstallDir.BeatSaberEXE);
                    if (File.Exists(beatSaberExe) == false)
                    {
                        Console.WriteLine($"Could not find '{beatSaberExe}'. InstallDirectory: '{InstallDirectory}'");
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
                string libs = @"Libs";
                string managed = @"Beat Saber_Data\Managed";

                string libsDir = Path.Combine(InstallDirectory, libs);
                string managedDir = Path.Combine(InstallDirectory, managed);

                Logger.Log("Resolving Beat Saber version");
                string version = VersionFinder.FindVersion(InstallDirectory);

                string outDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "stripped", version);
                string outLibs = Path.Combine(outDir, libs);
                string outManaged = Path.Combine(outDir, managed);
                Logger.Log("Creating output directory");
                Directory.CreateDirectory(outDir);
                Directory.CreateDirectory(outManaged);
                Directory.CreateDirectory(outLibs);

                string[] whitelist = new string[]
                {
                    "IPA.",
                    "TextMeshPro",
                    "UnityEngine.",
                    "Unity.",
                    "VivaDock",
                    "Mono.",
                    "Ookii.",
                    "LIV",
                    "Accessibility",
                    "MediaLoader",
                    "I18N",
                    "Assembly-CSharp",
                    "Main",
                    "Cinemachine",
                    "Colors",
                    "Core",
                    "DynamicBone",
                    "FinalIK",
                    "Oculus",
                    "Steam",
                    "HMLib",
                    "HMUI",
                    "Rendering",
                    "VRUI",
                    "Zenject",
                    "Polyglot",
                    "netstandard",
                    "0Harmony",
                    "Newtonsoft.Json",
                    "SemVer"
                };

                string[] blacklist = new string[]
                {
                    "System.Core.dll"
                };

                foreach (string f in ResolveDLLs(managedDir, whitelist, blacklist))
                {
                    StripDLL(f, outManaged, libsDir, managedDir);
                }

                foreach (string f in ResolveDLLs(libsDir, whitelist, blacklist))
                {
                    StripDLL(f, outLibs, libsDir, managedDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
            Console.WriteLine("Finished");
            Console.ReadKey();
        }

        internal static string[] ResolveDLLs(string managedDir, string[] whitelist, string[] blacklist)
        {
            List<string> acceptedFiles = new List<string>();
            var filePaths = Directory.GetFiles(managedDir);
            foreach (var filePath in filePaths)
            {
                FileInfo file = new FileInfo(filePath);
                if (file.Extension != ".dll")
                    continue;
                bool passedWhitelist = false;
                foreach (var whiteListItem in whitelist)
                {
                    if(file.Name.Contains(whiteListItem))
                    {
                        passedWhitelist = true;
                        break;
                    }
                }
                    
                if (passedWhitelist)
                {
                    if (!blacklist.Any(b => b.Contains(file.Name)))
                        acceptedFiles.Add(filePath);
                    else
                        Console.WriteLine($"Skipping {file.Name}, is blacklisted.");
                }
                else
                    Console.WriteLine($"Skipping {file.Name}, not in the whitelist.");
            }

            return acceptedFiles.ToArray();
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
