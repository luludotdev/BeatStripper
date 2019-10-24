using System;
using System.IO;
using System.Text;

namespace BeatStripper
{
    internal static class VersionFinder
    {
        internal static string FindVersion(string installDir)
        {
            string managersPath = Path.Combine(installDir, @"Beat Saber_Data", @"globalgamemanagers");
            if (File.Exists(managersPath) == false)
            {
                throw new FileNotFoundException();
            }

            using (var fileStream = new FileStream(managersPath, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = File.ReadAllBytes(managersPath);
                byte[] versionBytes = new byte[16];

                fileStream.Read(bytes, 0, Convert.ToInt32(fileStream.Length));
                fileStream.Close();

                int sourceIndex = Encoding.Default.GetString(bytes).IndexOf("public.app-category.games") + 136;
                Array.Copy(bytes, sourceIndex, versionBytes, 0, 16);

                return Encoding.Default.GetString(versionBytes).Trim(IllegalCharacters).Trim();
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
