using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UiPathTeam.PDFRedaction.Activities.Workaround
{
    public static class NativeLibrariesWorkarounds
    {
        private static string PdfiumRelativePath => GetDllPath("pdfium.dll");
        private static string SkiaSharpRelativePath => GetDllPath("libSkiaSharp.dll");

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        public static void Load(bool isWindows = true)
        {
            if (!isWindows) return;

            var assemblyLocation = GetAssemblyLocation();
            LoadDllLibrary(Path.Combine(assemblyLocation, PdfiumRelativePath));
            LoadDllLibrary(Path.Combine(assemblyLocation, SkiaSharpRelativePath));
        }

        private static void LoadDllLibrary(string path)
        {
            IntPtr handle = LoadLibrary(path);
            if (handle == IntPtr.Zero)
            {
                throw new Exception($"Failed to load library {path} (ErrorCode: {Marshal.GetLastWin32Error()})");
            }
        }

        private static string GetDllPath(string dllName)
        {
#if NETCOREAPP
            return Path.Combine("..", "runtimes", "win-x64", "native", dllName);
#elif NETFRAMEWORK
            return Path.Combine("..", "runtimes", Environment.Is64BitProcess ? "win-x64" : "win-x86", "native", dllName);
#endif
        }

        private static string GetAssemblyLocation()
        {
#if NETCOREAPP
            return typeof(NativeLibrariesWorkarounds).Assembly.Location;
#else
            var codeBase = typeof(NativeLibrariesWorkarounds).Assembly.CodeBase.Replace("file:///", "");
            return codeBase.StartsWith("/") && !codeBase.StartsWith("//") ? "/" + codeBase : codeBase;
#endif
        }
    }
}
