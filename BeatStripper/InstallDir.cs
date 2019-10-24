using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace BeatStripper
{
    // Shamelessly stolen from ModAssistant

    internal class InstallDir
    {
        internal const string BeatSaberAPPID = "620980";
        internal const string BeatSaberEXE = "Beat Saber.exe";
        internal const string IPA_EXE = "IPA.exe";

        public static string GetInstallDir()
        {
            string InstallDir = null;

            if (!string.IsNullOrEmpty(InstallDir)
                && Directory.Exists(InstallDir)
                && Directory.Exists(Path.Combine(InstallDir, "Beat Saber_Data", "Plugins"))
                && File.Exists(Path.Combine(InstallDir, "Beat Saber.exe")))
            {
                return InstallDir;
            }

            try
            {
                InstallDir = GetSteamDir();
            }
            catch { }

            if (!string.IsNullOrEmpty(InstallDir))
                return InstallDir;

            try
            {
                InstallDir = GetOculusDir();
            }
            catch { }

            if (!String.IsNullOrEmpty(InstallDir))
                return InstallDir;

            return null;
        }

        public static string GetSteamDir()
        {
            string SteamInstall = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)?.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Valve")?.OpenSubKey("Steam")?.GetValue("InstallPath").ToString();
            if (string.IsNullOrEmpty(SteamInstall))
            {
                SteamInstall = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Valve")?.OpenSubKey("Steam")?.GetValue("InstallPath").ToString();
            }

            if (string.IsNullOrEmpty(SteamInstall)) return null;

            string vdf = Path.Combine(SteamInstall, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(@vdf)) return null;

            Regex regex = new Regex("\\s\"\\d\"\\s+\"(.+)\"");
            List<string> SteamPaths = new List<string>
            {
                Path.Combine(SteamInstall, @"steamapps")
            };

            using (StreamReader reader = new StreamReader(@vdf))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        SteamPaths.Add(Path.Combine(match.Groups[1].Value.Replace(@"\\", @"\"), @"steamapps"));
                    }
                }
            }

            regex = new Regex("\\s\"installdir\"\\s+\"(.+)\"");
            foreach (string path in SteamPaths)
            {
                if (File.Exists(Path.Combine(@path, @"appmanifest_" + BeatSaberAPPID + ".acf")))
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(@path, @"appmanifest_" + BeatSaberAPPID + ".acf")))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Match match = regex.Match(line);
                            if (match.Success)
                            {
                                if (File.Exists(Path.Combine(@path, @"common", match.Groups[1].Value, "Beat Saber.exe")))
                                {
                                    return Path.Combine(@path, @"common", match.Groups[1].Value);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static string GetOculusDir()
        {
            string OculusInstall = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)?.OpenSubKey("SOFTWARE")?.OpenSubKey("Wow6432Node")?.OpenSubKey("Oculus VR, LLC")?.OpenSubKey("Oculus")?.OpenSubKey("Config")?.GetValue("InitialAppLibrary").ToString();
            if (string.IsNullOrEmpty(OculusInstall)) return null;

            if (!string.IsNullOrEmpty(OculusInstall))
            {
                if (File.Exists(Path.Combine(OculusInstall, "Software", "hyperbolic-magnetism-beat-saber", "Beat Saber.exe")))
                {
                    return Path.Combine(OculusInstall, "Software", "hyperbolic-magnetism-beat-saber");
                }
            }

            using (RegistryKey librariesKey = Registry.CurrentUser.OpenSubKey("Software")?.OpenSubKey("Oculus VR, LLC")?.OpenSubKey("Oculus")?.OpenSubKey("Libraries"))
            {
                WqlObjectQuery wqlQuery = new WqlObjectQuery("SELECT * FROM Win32_Volume");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wqlQuery);
                Dictionary<string, string> guidLetterVolumes = new Dictionary<string, string>();

                foreach (ManagementBaseObject disk in searcher.Get())
                {
                    var diskId = ((string)disk.GetPropertyValue("DeviceID")).Substring(11, 36);
                    var diskLetter = ((string)disk.GetPropertyValue("DriveLetter")) + @"\";

                    if (!string.IsNullOrWhiteSpace(diskLetter))
                        guidLetterVolumes.Add(diskId, diskLetter);
                }

                foreach (string libraryKeyName in librariesKey.GetSubKeyNames())
                {
                    using (RegistryKey libraryKey = librariesKey.OpenSubKey(libraryKeyName))
                    {
                        string libraryPath = (string)libraryKey.GetValue("Path");
                        string GUIDLetter = guidLetterVolumes.FirstOrDefault(x => libraryPath.Contains(x.Key)).Value;

                        if (string.IsNullOrEmpty(GUIDLetter) == false)
                        {
                            string finalPath = Path.Combine(GUIDLetter, libraryPath.Substring(49), @"Software\hyperbolic-magnetism-beat-saber");
                            if (File.Exists(Path.Combine(finalPath, "Beat Saber.exe")))
                            {
                                return finalPath;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
