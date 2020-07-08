using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.DotNet.Csv;
using Microsoft.DotNet.Scanner;
using System.Threading.Tasks;

namespace ex_scan
{
    

    internal static class Program
    {
        class Package {
            public string BaselineVersion { get; set; }
            public List<string> StableVersions { get; set; }
            public Dictionary<string,string> InboxOn { get; set; }
        }

        class PackageIndex {
            public Dictionary<string,Package> Packages { get; set; }
            public bool IsInbox (IAssembly assembly, string tfm)
            {
                if (Packages == null)
                    return true;

                var inbox = false;
                var found = false;
                if (Packages.TryGetValue (assembly.Name.Value /* <- no bueno */, out var package))
                {   
                    found = true;
                    inbox = package.InboxOn?.ContainsKey (tfm) ?? false;
                }
                Console.WriteLine($"name: {assembly.Name.Value}, key: {assembly.Name.UniqueKey} inbox: {inbox}, found: {found}");
                return inbox;
            }
        }

        private static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                var toolName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
                Console.Error.WriteLine($"Usage: {toolName} <directory-or-binary> <out-path>");
                return 1;
            }

            var packages = new PackageIndex ();
            if (args.Length == 3)
            {
                using (FileStream fs = File.OpenRead(args[2]))
                {
                    packages = await JsonSerializer.DeserializeAsync<PackageIndex>(fs);
                    Console.WriteLine ($"count = {packages.Packages.Keys.Count()}");
                }
            }

            var inputPath = args[0];
            var outputPath = args[1];
            var isFile = File.Exists(inputPath);
            var isDirectory = Directory.Exists(inputPath);
            if (!isFile && !isDirectory)
            {
                Console.Error.WriteLine($"ERROR: '{inputPath}' must be a file or directory.");
                return 1;
            }

            try
            {
                Run(inputPath, outputPath, packages);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        }

        private static void Run(string inputPath, string outputPath, PackageIndex index)
        {
            var assemblies = LoadAssemblies(inputPath);

            using (var textWriter = new StreamWriter(outputPath))
            {
                var csvWriter = new CsvWriter(textWriter);
                var reporter = new CsvReporter(csvWriter);
                var scanner = new ExceptionScanner(reporter);

                foreach (var assembly in assemblies)
                    if (index.IsInbox (assembly, "net5.0"))
                        scanner.ScanAssembly(assembly);
            }
        }

        private static IEnumerable<IAssembly> LoadAssemblies(string input)
        {
            var inputPaths = HostEnvironment.SplitPaths(input);
            var filePaths = HostEnvironment.GetFilePaths(inputPaths, SearchOption.AllDirectories).ToArray();
            return HostEnvironment.LoadAssemblySet(filePaths).Distinct();
        }
    }
}
