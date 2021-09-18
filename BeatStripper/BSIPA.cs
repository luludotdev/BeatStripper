using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using SimpleJSON;

namespace BeatStripper
{
    internal class BSIPA
    {
        private static readonly HttpClient client = new HttpClient();

        internal const string RepoSlug = "bsmg/BeatSaber-IPA-Reloaded";
        internal const string EXE = "IPA.exe";
        internal const string WinHTTPDll = "winhttp.dll";

        private static async Task<string> GetReleaseURLAsync()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "BeatStripper");
            string url = $"https://api.github.com/repos/{RepoSlug}/releases/latest";

            HttpResponseMessage resp = await client.GetAsync(url);
            string body = await resp.Content.ReadAsStringAsync();

            var json = JSON.Parse(body);
            List<string> urls = new List<string>();

            foreach (var x in json["assets"].AsArray)
            {
                string name = x.Value["name"];
                string assetURL = x.Value["browser_download_url"];

                if (name == "BSIPA-x64-Net4.zip") return assetURL;
            }

            return null;
        }

        public static async Task<byte[]> GetReleaseZipAsync()
        {
            string url = await GetReleaseURLAsync();
            HttpResponseMessage resp = await client.GetAsync(url);

            byte[] body = await resp.Content.ReadAsByteArrayAsync();
            return body;
        }

        public static byte[] GetReleaseZip()
        {
            Task<byte[]> task = GetReleaseZipAsync();
            task.Wait();

            return task.Result;
        }

        private static bool HasBSIPA(string path)
        {
            string exePath = Path.Combine(path, EXE);
            return File.Exists(exePath);
        }

        public static bool EnsureExists(string path)
        {
            if (HasBSIPA(path)) return true;

            byte[] zip = GetReleaseZip();
            string tempZipPath = Path.Combine(path, "BSIPA.zip");
            File.WriteAllBytes(tempZipPath, zip);

            ZipFile.ExtractToDirectory(tempZipPath, path);

            File.Delete(tempZipPath);
            return false;
        }

        public static bool IsPatched(string path)
        {
            string exePath = Path.Combine(path, WinHTTPDll);
            return File.Exists(exePath);
        }

        private static async Task PatchDirAsync(string path)
        {
            await Task.Run(() =>
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(path, EXE),
                    WorkingDirectory = path,
                    Arguments = "-n"
                }).WaitForExit()
            );
        }

        public static void PatchDir(string path)
        {
            Task task = PatchDirAsync(path);
            task.Wait();
        }
    }
}
