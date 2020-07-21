using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Cci;
using Microsoft.Cci.Extensions;
using Microsoft.DotNet.Scanner;
using System.Collections.Generic;

using CsvHelper;

namespace scan_bcl
{
    class Program
    {
        const string OutputDirSwitch = "-out";
        const string threeBCLSwitch = "-tbcl";
        const string fiveBCLSwitch = "-fbcl";

        static int Main(string[] args)
        {
            if (!TryParseArguments(args, out string threeBCLPath, out string fiveBCLPath, out string outputDirPath)) {
                var toolName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);
                Console.Error.WriteLine($"Usage: {toolName} [{OutputDirSwitch}:<output-path>] [{threeBCLSwitch}:<three-bcl-path>] [{fiveBCLSwitch}:<five-bcl-path>]");
                Console.Error.WriteLine($"\nMandatory:\n");
                Console.Error.WriteLine($"\t{OutputDirSwitch}:<out-path>");
                Console.Error.WriteLine($"\t{threeBCLPath}:<three-bcl-path>");
                Console.Error.WriteLine($"\t{fiveBCLPath}:<five-bcl-path>");
                Console.Error.WriteLine();
                return 1;
            }
           
            Run (threeBCLPath, fiveBCLPath, outputDirPath);
            return 0;
            
        }

        private static bool TryParseArguments (string[] args, out string threeBCLPath, out string fiveBCLPath, out string outputDirPath)
        {
            const int minArgsLength = 1;
            const int maxArgsLength = 3;

            threeBCLPath = fiveBCLPath = outputDirPath = null;
            if (args.Length < minArgsLength || args.Length > maxArgsLength)
                return false;
            
            for (var i = 0; i < args.Length; ++i)
            {
                var tokens = args[i].Split(":");
                if (tokens.Length != 2) 
                    return false;

                var fullPath = Path.GetFullPath(tokens[1]);
                switch (tokens[0])
                {
                    case OutputDirSwitch:
                        outputDirPath = fullPath;
                        break;
                    case threeBCLSwitch:
                        threeBCLPath = fullPath;
                        break;
                    case fiveBCLSwitch:
                        fiveBCLPath = fullPath;
                        break;
                    default:
                        return false;
                }
            }
            return !string.IsNullOrWhiteSpace(outputDirPath);
        }

        private static void Run (string threeBCLPath, string fiveBCLPath, string outputDirPath) {
            if (!Directory.Exists(threeBCLPath)) 
                throw new DirectoryNotFoundException($"BCL Path not found at : {threeBCLPath}");
            if (!Directory.Exists(fiveBCLPath)) 
                throw new DirectoryNotFoundException($"BCL Path not found at : {fiveBCLPath}");
            if (!Directory.Exists(outputDirPath)) {
                Directory.CreateDirectory(outputDirPath);
            }

            var outputPath = Path.Combine(outputDirPath, "diff.csv");
            var diffDatabase = new Database ();
            RunScanBCL(threeBCLPath, diffDatabase, "3.2.0");
            RunScanBCL (fiveBCLPath, diffDatabase, "5.0");
            ExportCsv(diffDatabase, outputPath);
        }

        private static void RunScanBCL (string BCLPath, Database database, string api) {
            var assemblies = LoadAssemblies (BCLPath);

            var reporter = new DatabaseReporter(database, api);
            var scanner = new ExceptionScanner(reporter);

            foreach (var assembly in assemblies)
            {
                scanner.ScanAssembly(assembly);
            }
        }

        private static IEnumerable<IAssembly> LoadAssemblies(string input)
        {
            var inputPaths = HostEnvironment.SplitPaths(input);
            var filePaths = HostEnvironment.GetFilePaths(inputPaths, SearchOption.AllDirectories).ToArray();
            return HostEnvironment.LoadAssemblySet(filePaths).Distinct();
        }

        private static void ExportCsv (Database database, string path)
        {
            using (var streamWriter = new StreamWriter(path))
            using (var writer = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
            {

                var entries = database.Entries.OrderBy(e=> e.Api)
                                                    .ThenBy(e => e.Namespace)
                                                    .ThenBy(e => e.Type)
                                                    .ThenBy(e => e.Member)
                                                    .ThenBy(e => e.DocId)
                                                    .ThenBy(e => e.Nesting);
                
                writer.WriteRecords(entries);
            }
        } 
    }
}
