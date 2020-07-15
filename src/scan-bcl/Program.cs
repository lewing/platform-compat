using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

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
            var path = Path.Combine(Directory.GetCurrentDirectory(), "../..");
            var scanPath = Path.Combine(path, "artifacts/bin/ex-scan/Debug/net461/ex-scan.exe");

            if (!File.Exists(scanPath)) {
                Console.WriteLine(scanPath);
                throw new FileNotFoundException($"Assembly scanner ex-scan.exe not found at: {scanPath}");
            }

            if (!Directory.Exists(threeBCLPath)) 
                throw new DirectoryNotFoundException($"BCL Path not found at : {threeBCLPath}");
            if (!Directory.Exists(fiveBCLPath)) 
                throw new DirectoryNotFoundException($"BCL Path not found at : {fiveBCLPath}");
            if (!Directory.Exists(outputDirPath)) {
                Directory.CreateDirectory(outputDirPath);
            }

            var threeOutputPath = Path.Combine(outputDirPath, "three-bcl.csv");
            var threeScan = ScanBCL (scanPath, threeBCLPath, threeOutputPath);

            var fiveOutputPath = Path.Combine(outputDirPath, "five-bcl.csv");
            var fiveScan = ScanBCL (scanPath, fiveBCLPath, fiveOutputPath);

            if (threeScan && fiveScan) {
                var diff = Scan (threeOutputPath, fiveOutputPath);
                var outputPath = Path.Combine(outputDirPath, "diff.csv");
                ExportCsv(diff, outputPath);
            }
        }

        private static bool ScanBCL (string scanPath, string BCLPath, string outputPath) {
            var command = "mono";
            var args = $@"{scanPath} {BCLPath} {outputPath}";

            var process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.OutputDataReceived += (s, d) => Console.Out.WriteLine(d.Data);
            process.ErrorDataReceived += (s, d) => Console.Error.WriteLine(d.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private static Database Scan (string threeOutputPath, string fiveOutputPath) {
            Console.WriteLine("Analyzing...");

            Database threeBCLDatabase = null;
            var diff = new Database ();
            if (threeOutputPath != null && fiveOutputPath != null) {
                threeBCLDatabase = new Database ();
                ImportCsv(threeOutputPath, threeBCLDatabase);
                ImportCsv(fiveOutputPath, threeBCLDatabase);

                diff = threeBCLDatabase;
            }
            return diff;
        }

        private static void ImportCsv(string path, Database database)
        {
             
            using (var streamReader = new StreamReader(path))
            using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
            {
                // csv.Configuration.RegisterClassMap<DatabaseMap>();
                csvReader.Read();
                var header = csvReader.ReadHeader();
                while (csvReader.Read()) {
                    var row = csvReader.GetRecord<DatabaseEntry>();
                    DatabaseEntry query = null;
                    try{
                        query = database.Entries.Single(x => row.DocId == x.DocId && 
                                row.Namespace == x.Namespace && row.Type == x.Type &&
                                row.Member == x.Member);
                    } catch (System.InvalidOperationException) {
                        database.Add(row.DocId, row.Namespace, row.Type, row.Member, row.Nesting);
                    }
                    if (query != null) {
                        database.Remove(row.DocId);
                    }
                }
                              
            }
        }

        private static void ExportCsv (Database database, string path)
        {
            using (var streamWriter = new StreamWriter(path))
            using (var writer = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
            {

                var entries = database.Entries.OrderBy(e => e.Namespace)
                                                    .ThenBy(e => e.Type)
                                                    .ThenBy(e => e.Member)
                                                    .ThenBy(e => e.DocId)
                                                    .ThenBy(e => e.Nesting);
                
                writer.WriteRecords(entries);
            }
        } 
    }
}
