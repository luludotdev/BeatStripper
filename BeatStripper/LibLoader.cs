using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Cecil;

namespace BeatStripper
{
    internal class CecilLibLoader : BaseAssemblyResolver
    {
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            LibLoader.SetupAssemblyFilenames();

            if (LibLoader.FilenameLocations.TryGetValue($"{name.Name}.{name.Version}.dll", out var path))
            {
                if (File.Exists(path))
                {
                    return AssemblyDefinition.ReadAssembly(path, parameters);
                }
            }
            else if (LibLoader.FilenameLocations.TryGetValue($"{name.Name}.dll", out path))
            {
                if (File.Exists(path))
                {
                    return AssemblyDefinition.ReadAssembly(path, parameters);
                }
            }

            return base.Resolve(name, parameters);
        }
    }

    internal static class LibLoader
    {
        internal static string LibraryPath => Path.Combine(Program.InstallDirectory, "Libs");
        internal static string NativeLibraryPath => Path.Combine(LibraryPath, "Native");
        internal static Dictionary<string, string> FilenameLocations;

        internal static void Configure()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyLibLoader;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyLibLoader;
            SetupAssemblyFilenames(true);
        }

        internal static void SetupAssemblyFilenames(bool force = false)
        {
            if (FilenameLocations == null || force)
            {
                FilenameLocations = new Dictionary<string, string>();

                foreach (var fn in TraverseTree(LibraryPath, s => s != NativeLibraryPath))
                {
                    if (FilenameLocations.ContainsKey(fn.Name) == false)
                    {
                        FilenameLocations.Add(fn.Name, fn.FullName);
                    }
                }

                void AddDir(string path)
                {
                    var retPtr = AddDllDirectory(path);
                    if (retPtr == IntPtr.Zero)
                    {
                        var err = new Win32Exception();
                    }
                }

                if (Directory.Exists(NativeLibraryPath))
                {
                    AddDir(NativeLibraryPath);
                    TraverseTree(NativeLibraryPath, dir =>
                    { // this is a terrible hack for iterating directories
                        AddDir(dir); return true;
                    }).All(f => true); // force it to iterate all
                }

                var unityData = Directory.EnumerateDirectories(Program.InstallDirectory, "*_Data").First();
                AddDir(Path.Combine(unityData, "Plugins"));

                foreach (var dir in Environment.GetEnvironmentVariable("path").Split(Path.PathSeparator))
                {
                    AddDir(dir);
                }
            }
        }

        public static Assembly AssemblyLibLoader(object source, ResolveEventArgs e)
        {
            var asmName = new AssemblyName(e.Name);
            return LoadLibrary(asmName);
        }

        internal static Assembly LoadLibrary(AssemblyName asmName)
        {
            SetupAssemblyFilenames();

            var testFile = $"{asmName.Name}.{asmName.Version}.dll";

            if (FilenameLocations.TryGetValue(testFile, out var path))
            {
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            else if (FilenameLocations.TryGetValue(testFile = $"{asmName.Name}.dll", out path))
            {
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }

            return null;
        }

        private static IEnumerable<FileInfo> TraverseTree(string root, Func<string, bool> dirValidator = null)
        {
            if (dirValidator == null) dirValidator = s => true;

            Stack<string> dirs = new Stack<string>(32);

            if (!Directory.Exists(root))
                throw new ArgumentException();
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException)
                { continue; }
                catch (DirectoryNotFoundException)
                { continue; }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException)
                { continue; }
                catch (DirectoryNotFoundException)
                { continue; }

                foreach (string str in subDirs)
                    if (dirValidator(str)) dirs.Push(str);

                foreach (string file in files)
                {
                    FileInfo nextValue;
                    try
                    {
                        nextValue = new FileInfo(file);
                    }
                    catch (FileNotFoundException)
                    { continue; }

                    yield return nextValue;
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AddDllDirectory(string lpPathName);
    }
}
