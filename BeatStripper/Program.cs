using SimpleJSON;
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
                string libs = "Libs";
                string managed = Path.Combine("Beat Saber_Data", "Managed");
                string plugins = "Plugins";

                string libsSource = Path.Combine(InstallDirectory, libs);
                string managedSource = Path.Combine(InstallDirectory, managed);
                string pluginsSource = Path.Combine(InstallDirectory, plugins);

                Logger.Log("Resolving Beat Saber version");
                string version = VersionFinder.GetVersion(InstallDirectory);

                string outDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "stripped", $"BeatSaber_{version}");
                string outLibs = Path.Combine(outDir, libs);
                string outManaged = Path.Combine(outDir, managed);
                string outPlugins = Path.Combine(outDir, plugins);
                Logger.Log("Creating output directory");
                Directory.CreateDirectory(outDir);
                var extras = ParseExtras("extra-assemblies.json");
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
                    "BGNet",
                    "BouncyCastle.",
                    "LiteNetLib",
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
                string[] resolveDirs = { managedSource, libsSource, pluginsSource };
                ProcessDirectory(managedSource, outManaged, whitelist, blacklist, extras["Managed"], resolveDirs);
                ProcessDirectory(libsSource, outLibs, whitelist, blacklist, extras["Libs"], resolveDirs);
                ProcessDirectory(pluginsSource, outPlugins, whitelist, blacklist, extras["Plugins"], resolveDirs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
            Console.WriteLine("Finished");
            Console.ReadKey();
        }

        internal static Dictionary<string, string[]> ParseExtras(string filePath)
        {
            Dictionary<string, string[]> extras = new Dictionary<string, string[]>()
            {
                {"Managed", Array.Empty<string>() },
                {"Libs", Array.Empty<string>() },
                {"Plugins", Array.Empty<string>() }
            };
            if (!File.Exists(filePath))
            {
                Console.WriteLine("No extra assemblies config.");
                return extras;
            }
            try
            {
                string json = File.ReadAllText(filePath);
                JSONObject obj = (JSONObject)JSON.Parse(json);
                extras["Managed"] = GetExtras("Managed", obj);
                extras["Libs"] = GetExtras("Libs", obj);
                extras["Plugins"] = GetExtras("Plugins", obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing extra assemblies file: {ex.Message}");
            }
            return extras;
        }

        internal static string[] GetExtras(string name, JSONObject obj)
        {
            if (obj == null)
                return Array.Empty<string>();
            JSONArray ary = obj[name] as JSONArray;
            if (ary != null)
                return ary.Linq.Select(e => e.Value.Value).ToArray();
            return Array.Empty<string>();
        }

        internal static void ProcessDirectory(string sourceDir, string outDir, IEnumerable<string> whitelist,
            IEnumerable<string> blacklist, IEnumerable<string> extras, IEnumerable<string> resolveDirs)
        {
            HashSet<string> strippedFiles = new HashSet<string>();
            if (extras == null)
                extras = Array.Empty<string>();
            foreach (string f in ResolveDLLs(sourceDir, whitelist.Concat(extras), blacklist))
            {
                if (!strippedFiles.Add(f))
                {
                    Console.WriteLine($"Duplicate entry: already stripped '{f}'");
                    continue;
                }
                Directory.CreateDirectory(outDir);
                StripDLL(f, outDir, resolveDirs.ToArray());
            }
        }

        internal static string[] ResolveDLLs(string managedDir, IEnumerable<string> whitelist, IEnumerable<string> blacklist)
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
                    if (file.Name.Contains(whiteListItem))
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
            //mod.Virtualize(); // This could make the assemblies inconsistent with the actual game assemblies?
            mod.Strip();

            string outFile = Path.Combine(outDir, file.Name);
            mod.Write(outFile);
        }
    }
}
