using System;
using System.IO;
using System.Text;

namespace BeatStripper
{
    internal static class VersionFinder
    {
        public static string GetVersion(string installDir)
        {
            string filename = Path.Combine(installDir, "Beat Saber_Data", "globalgamemanagers");
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] file = File.ReadAllBytes(filename);
                byte[] bytes = new byte[64];

                fs.Read(file, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                int index = Encoding.UTF8.GetString(file).IndexOf("public.app-category.games") + 136;

                Array.Copy(file, index, bytes, 0, 64);
                string version = Encoding.UTF8.GetString(bytes).Trim(IllegalCharacters);

                return version;
            }
        }

        internal static readonly char[] IllegalCharacters = new char[]
            {
                '<',
                '>',
                ':',
                '/',
                '\\',
                '|',
                '?',
                '*',
                '"',
                '\0',
                '\u0001',
                '\u0002',
                '\u0003',
                '\u0004',
                '\u0005',
                '\u0006',
                '\a',
                '\b',
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                '\u000e',
                '\r',
                '\u000f',
                '\u0010',
                '\u0011',
                '\u0012',
                '\u0013',
                '\u0014',
                '\u0015',
                '\u0016',
                '\u0017',
                '\u0018',
                '\u0019',
                '\u001a',
                '\u001b',
                '\u001c',
                '\u001d',
                '\u001f'
            };
    }
}
