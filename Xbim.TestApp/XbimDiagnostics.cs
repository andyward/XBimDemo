using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Humanizer;

namespace Xbim.TestApp
{
    public static class XbimDiagnostics
    {

        public static void DumpVersions()
        {
            Console.WriteLine();
            Console.WriteLine("Running under [{0}] : 64bit OS: {2}\nProcess type:{1}",
                Environment.OSVersion.VersionString,
                Environment.Is64BitProcess ? "64-bit" : "32-bit",
                Environment.Is64BitOperatingSystem);

            // Force Geometry engine to load if not already. Interop will load correct Managed C++ DLL
            var engine = new  Xbim.Geometry.Engine.Interop.XbimGeometryEngine();

            var ignoreList = new[] { "mscor", "system", "microsoft.", "vshost" };
            var xBimAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !ignoreList.Any(ignore => asm.GetName().Name.ToLowerInvariant().Contains(ignore)))
                .OrderBy(asm => asm.GetName().Name);

            Console.WriteLine("{0, -22} {1,-5} {2,-14} {3,-8} {4,-8} {5}",
                "Assembly",
                "Arch",
                "AssemVers",
                "FileVers",
                "ProdVers",
                "Created");
            Console.WriteLine("{0, -22} {1,-5} {2,-14} {3,-8} {4,-8} {5}",
                new string('=', 22),
                new string('=', 5),
                new string('=', 14),
                new string('=', 8),
                new string('=', 8),
                new string('=', 12));
            foreach (var assembly in xBimAssemblies)
            {
                ShowVersion(assembly);
            }

        }

        private static void ShowVersion(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            var fileVersion = GetFileVersion(assembly);
            Console.WriteLine("{0, -22} {1,-5} {2,-14} {3,-8} {4,-8} {5:dd/MM/yyyy hh:mm}",
                assemblyName.Name.Truncate(22, Truncator.FixedLength,TruncateFrom.Left),
                assemblyName.ProcessorArchitecture.ToString(),
                assemblyName.Version.ToString(),
                fileVersion.FileVersion,
                fileVersion.ProductVersion,
                GetDateCreated(assembly)
                );
        }

        private static FileVersionInfo GetFileVersion(Assembly assembly)
        {
            return FileVersionInfo.GetVersionInfo(assembly.Location);

        }

        private static DateTime GetDateCreated(Assembly assembly)
        {
            return new FileInfo(assembly.Location).LastWriteTime;

        }
    }
}
