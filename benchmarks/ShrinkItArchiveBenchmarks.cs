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

    // --- Header parsing: Stream path ---

    [Benchmark(Description = "Parse headers (small, stream)")]
    [BenchmarkCategory("Headers", "Stream")]
    public ShrinkItArchive ParseHeaders_Small_Stream()
    {
        using var stream = new MemoryStream(_smallArchiveData);
        return new ShrinkItArchive(stream);
    }

    [Benchmark(Description = "Parse headers (medium, stream)")]
    [BenchmarkCategory("Headers", "Stream")]
    public ShrinkItArchive ParseHeaders_Medium_Stream()
    {
        using var stream = new MemoryStream(_mediumArchiveData);
        return new ShrinkItArchive(stream);
    }

    [Benchmark(Description = "Parse headers (large, stream)")]
    [BenchmarkCategory("Headers", "Stream")]
    public ShrinkItArchive ParseHeaders_Large_Stream()
    {
        using var stream = new MemoryStream(_largeArchiveData);
        return new ShrinkItArchive(stream);
    }

    // --- Header parsing: Memory path ---

    [Benchmark(Description = "Parse headers (small, memory)")]
    [BenchmarkCategory("Headers", "Memory")]
    public ShrinkItArchive ParseHeaders_Small_Memory()
    {
        return new ShrinkItArchive((ReadOnlyMemory<byte>)_smallArchiveData);
    }

    [Benchmark(Description = "Parse headers (medium, memory)")]
    [BenchmarkCategory("Headers", "Memory")]
    public ShrinkItArchive ParseHeaders_Medium_Memory()
    {
        return new ShrinkItArchive((ReadOnlyMemory<byte>)_mediumArchiveData);
    }

    [Benchmark(Description = "Parse headers (large, memory)")]
    [BenchmarkCategory("Headers", "Memory")]
    public ShrinkItArchive ParseHeaders_Large_Memory()
    {
        return new ShrinkItArchive((ReadOnlyMemory<byte>)_largeArchiveData);
    }

    // --- Extraction: Stream path (GetDataFork) ---

    [Benchmark(Description = "Extract all data forks (small, LZW/1, stream)")]
    [BenchmarkCategory("Extract", "Stream")]
    public long ExtractAllDataForks_Small_Stream()
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

    [Benchmark(Description = "Extract all data forks (medium, LZW/2, stream)")]
    [BenchmarkCategory("Extract", "Stream")]
    public long ExtractAllDataForks_Medium_Stream()
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

    [Benchmark(Description = "Extract all data forks (large, stream)")]
    [BenchmarkCategory("Extract", "Stream")]
    public long ExtractAllDataForks_Large_Stream()
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

    // --- Extraction: Memory path (GetDataFork) ---

    [Benchmark(Description = "Extract all data forks (small, LZW/1, memory)")]
    [BenchmarkCategory("Extract", "Memory")]
    public long ExtractAllDataForks_Small_Memory()
    {
        var archive = new ShrinkItArchive((ReadOnlyMemory<byte>)_smallArchiveData);
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var data = archive.GetDataFork(entry);
            if (data != null)
                totalBytes += data.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "Extract all data forks (medium, LZW/2, memory)")]
    [BenchmarkCategory("Extract", "Memory")]
    public long ExtractAllDataForks_Medium_Memory()
    {
        var archive = new ShrinkItArchive((ReadOnlyMemory<byte>)_mediumArchiveData);
        long totalBytes = 0;
        foreach (var entry in archive.Entries)
        {
            var data = archive.GetDataFork(entry);
            if (data != null)
                totalBytes += data.Length;
        }
        return totalBytes;
    }

    [Benchmark(Description = "Extract all data forks (large, memory)")]
    [BenchmarkCategory("Extract", "Memory")]
    public long ExtractAllDataForks_Large_Memory()
    {
        var archive = new ShrinkItArchive((ReadOnlyMemory<byte>)_largeArchiveData);
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
