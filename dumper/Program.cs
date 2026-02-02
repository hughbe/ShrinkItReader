using Spectre.Console;
using Spectre.Console.Cli;
using ShrinkItReader;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<ExtractCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("shrinkit-dumper");
            config.ValidateExamples();
        });

        return app.Run(args);
    }
}

sealed class ExtractSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    public required string Input { get; init; }

    [CommandOption("-o|--output")]
    public string? Output { get; init; }
}

sealed class ExtractCommand : AsyncCommand<ExtractSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        var outputPath = settings.Output ?? Path.GetFileNameWithoutExtension(input.Name);
        var outputDir = new DirectoryInfo(outputPath);
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        await using var stream = input.OpenRead();
        var archive = new ShrinkItArchive(stream);

        AnsiConsole.MarkupLine($"[cyan]Archive:[/] {input.Name}");
        
        // Show Binary II wrapper info if present
        if (archive.BinaryIIHeader.HasValue)
        {
            var binaryII = archive.BinaryIIHeader.Value;
            AnsiConsole.MarkupLine($"[cyan]Binary II Wrapper:[/] Yes (version {binaryII.Version})");
            AnsiConsole.MarkupLine($"[cyan]  Wrapped File:[/] {binaryII.FileName}");
            AnsiConsole.MarkupLine($"[cyan]  File Type:[/] 0x{binaryII.FileType:X2}");
            AnsiConsole.MarkupLine($"[cyan]  Aux Type:[/] 0x{binaryII.AuxType:X4}");
        }
        
        AnsiConsole.MarkupLine($"[cyan]Total Records:[/] {archive.MasterHeaderBlock.TotalRecords}");
        AnsiConsole.MarkupLine($"[cyan]Output Directory:[/] {outputDir.FullName}");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Extracting files[/]", maxValue: archive.Entries.Count);

                foreach (var entry in archive.Entries)
                {
                    var fileName = SanitizeName(archive.GetFileName(entry));

                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"entry_{archive.Entries.IndexOf(entry)}";
                    }

                    AnsiConsole.MarkupLine($"[yellow]Extracting:[/] {fileName}");
                    
                    // Show file metadata
                    var header = entry.HeaderBlock;
                    AnsiConsole.MarkupLine($"  [dim]File System:[/] {header.FileSystemId}");
                    AnsiConsole.MarkupLine($"  [dim]Type:[/] 0x{header.FileType:X2}  [dim]Aux:[/] 0x{header.AuxType:X4}  [dim]Storage:[/] {header.StorageType}");
                    AnsiConsole.MarkupLine($"  [dim]Created:[/] {header.CreationDate}  [dim]Modified:[/] {header.LastModificationDate}");

                    // Extract disk image
                    var diskImageThread = entry.Threads.FirstOrDefault(t => t.Classification == ShrinkItThreadClassification.Data && t.Kind == 0x0001);
                    if (diskImageThread.CompressedDataSize > 0)
                    {
                        var diskImage = archive.GetDiskImage(entry);
                        if (diskImage != null)
                        {
                            var diskPath = Path.Combine(outputDir.FullName, $"{fileName}.dsk");
                            await File.WriteAllBytesAsync(diskPath, diskImage, cancellationToken);
                            var ratio = diskImageThread.UncompressedDataSize > 0 
                                ? (1.0 - (double)diskImageThread.CompressedDataSize / diskImageThread.UncompressedDataSize) * 100 
                                : 0;
                            AnsiConsole.MarkupLine($"  [dim]Disk image:[/] {diskPath} ({FormatSize(diskImage.Length)}) [{diskImageThread.Format}, {ratio:F1}% saved]");
                        }
                    }

                    // Extract data fork
                    var dataThread = entry.Threads.FirstOrDefault(t => t.Classification == ShrinkItThreadClassification.Data && t.Kind == 0x0000);
                    if (dataThread.CompressedDataSize > 0)
                    {
                        var dataFork = archive.GetDataFork(entry);
                        if (dataFork != null)
                        {
                            var dataPath = Path.Combine(outputDir.FullName, fileName);
                            await File.WriteAllBytesAsync(dataPath, dataFork, cancellationToken);
                            var ratio = dataThread.UncompressedDataSize > 0 
                                ? (1.0 - (double)dataThread.CompressedDataSize / dataThread.UncompressedDataSize) * 100 
                                : 0;
                            AnsiConsole.MarkupLine($"  [dim]Data fork:[/] {dataPath} ({FormatSize(dataFork.Length)}) [{dataThread.Format}, {ratio:F1}% saved]");
                        }
                    }

                    // Extract resource fork
                    var resourceThread = entry.Threads.FirstOrDefault(t => t.Classification == ShrinkItThreadClassification.Data && t.Kind == 0x0002);
                    if (resourceThread.CompressedDataSize > 0)
                    {
                        var resourceFork = archive.GetResourceFork(entry);
                        if (resourceFork != null)
                        {
                            var resourcePath = Path.Combine(outputDir.FullName, $"{fileName}.rsrc");
                            await File.WriteAllBytesAsync(resourcePath, resourceFork, cancellationToken);
                            var ratio = resourceThread.UncompressedDataSize > 0 
                                ? (1.0 - (double)resourceThread.CompressedDataSize / resourceThread.UncompressedDataSize) * 100 
                                : 0;
                            AnsiConsole.MarkupLine($"  [dim]Resource fork:[/] {resourcePath} ({FormatSize(resourceFork.Length)}) [{resourceThread.Format}, {ratio:F1}% saved]");
                        }
                    }

                    task.Increment(1);
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Extraction complete[/]: {outputDir.FullName}");
        return 0;
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string SanitizeName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var invalidChar in invalidChars)
        {
            name = name.Replace(invalidChar, '_');
        }

        return name;
    }
}
