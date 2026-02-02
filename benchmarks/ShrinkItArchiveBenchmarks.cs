using BenchmarkDotNet.Attributes;
using ShrinkItReader;

namespace ShrinkItReader.Benchmarks;

/// <summary>
/// Benchmarks for reading and enumerating ShrinkIt archives.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ShrinkItArchiveBenchmarks
{
    private byte[] _smallArchiveData = null!;
    private byte[] _mediumArchiveData = null!;
    private byte[] _largeArchiveData = null!;

    // Small archive with LZW/1 compression
    private const string SmallArchivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/BSC-It.2.1.shk";
    // Medium archive with LZW/2 compression
    private const string MediumArchivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/getshk.200.shk";
    // Larger archive for stress testing
    private const string LargeArchivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/UNZIP41.SHK";

    [GlobalSetup]
    public void Setup()
    {
        _smallArchiveData = File.ReadAllBytes(SmallArchivePath);
        _mediumArchiveData = File.ReadAllBytes(MediumArchivePath);
        _largeArchiveData = File.ReadAllBytes(LargeArchivePath);
    }

    [Benchmark(Description = "Parse archive headers (small)")]
    public ShrinkItArchive ParseHeaders_Small()
    {
        using var stream = new MemoryStream(_smallArchiveData);
        return new ShrinkItArchive(stream);
    }

    [Benchmark(Description = "Parse archive headers (medium)")]
    public ShrinkItArchive ParseHeaders_Medium()
    {
        using var stream = new MemoryStream(_mediumArchiveData);
        return new ShrinkItArchive(stream);
    }

    [Benchmark(Description = "Parse archive headers (large)")]
    public ShrinkItArchive ParseHeaders_Large()
    {
        using var stream = new MemoryStream(_largeArchiveData);
        return new ShrinkItArchive(stream);
    }

    [Benchmark(Description = "Extract all data forks (small, LZW/1)")]
    public long ExtractAllDataForks_Small()
    {
        using var stream = new MemoryStream(_smallArchiveData);
        var archive = new ShrinkItArchive(stream);
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var data = archive.GetDataFork(entry);
            if (data != null)
                totalBytes += data.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "Extract all data forks (medium, LZW/2)")]
    public long ExtractAllDataForks_Medium()
    {
        using var stream = new MemoryStream(_mediumArchiveData);
        var archive = new ShrinkItArchive(stream);
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var data = archive.GetDataFork(entry);
            if (data != null)
                totalBytes += data.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "Extract all data forks (large)")]
    public long ExtractAllDataForks_Large()
    {
        using var stream = new MemoryStream(_largeArchiveData);
        var archive = new ShrinkItArchive(stream);
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var data = archive.GetDataFork(entry);
            if (data != null)
                totalBytes += data.Length;
        }
        return totalBytes;
    }
}
