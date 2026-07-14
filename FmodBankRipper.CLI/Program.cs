using FmodBankRipper.Core;
using System.CommandLine;

namespace FmodBankRipper.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("FMOD Bank Audio Extractor - Extracts .wav audio from FMOD bank files");

        var inputOption = new Option<string>(
            aliases: new[] { "--input", "-i" },
            description: "Input file or directory containing .bank/.fsb files")
        { IsRequired = true };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for extracted audio",
            getDefaultValue: () => "ExtractedAudio");

        var recursiveOption = new Option<bool>(
            aliases: new[] { "--recursive", "-r" },
            description: "Process subdirectories recursively",
            getDefaultValue: () => false);

        var overwriteOption = new Option<bool>(
            aliases: new[] { "--overwrite", "-w" },
            description: "Overwrite existing files",
            getDefaultValue: () => false);

        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(recursiveOption);
        rootCommand.AddOption(overwriteOption);

        rootCommand.SetHandler(async (string input, string output, bool recursive, bool overwrite) =>
        {
            var options = new ExtractionOptions
            {
                InputPath = input,
                OutputDirectory = output,
                Recursive = recursive,
                OverwriteExisting = overwrite
            };

            var extractor = new BankExtractor();
            extractor.ProgressChanged += (s, e) =>
            {
                if (e.TotalFiles > 0)
                {
                    Console.WriteLine($"[{e.CurrentFileIndex}/{e.TotalFiles}] {e.StatusMessage}");
                }
            };

            Console.WriteLine("=== FMOD Bank Audio Extractor ===");
            Console.WriteLine($"Input:  {input}");
            Console.WriteLine($"Output: {output}");
            Console.WriteLine();

            var results = await extractor.ExtractAsync(options);

            Console.WriteLine();
            Console.WriteLine("=== Extraction Complete ===");

            int totalExtracted = results.Sum(r => r.FilesExtracted);
            int totalFailed = results.Sum(r => r.FilesFailed);

            Console.WriteLine($"Files processed: {results.Count}");
            Console.WriteLine($"Samples extracted: {totalExtracted}");
            Console.WriteLine($"Samples failed: {totalFailed}");

            foreach (var result in results.Where(r => r.Errors.Any()))
            {
                Console.WriteLine();
                Console.WriteLine($"Errors in {Path.GetFileName(result.FilePath)}:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"  - {error}");
            }

            Environment.ExitCode = totalFailed > 0 ? 1 : 0;
        }, inputOption, outputOption, recursiveOption, overwriteOption);

        return await rootCommand.InvokeAsync(args);
    }
}