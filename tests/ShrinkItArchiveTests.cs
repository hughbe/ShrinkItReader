using System.Diagnostics;
using System.Security.Cryptography;

namespace ShrinkItReader.Tests;

public class AppleIIDiskTests
{
    public static TheoryData<string> DiskImages =>
    [
        "ftp.apple.asimov.net/images/disk_utils/archivers/Angel0.81b-an extractor for zip, lha,arc, zoo and Unix .z archives.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/BSC-It.2.1.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/Compress.1.1.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/Compress2.4.3.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/ENCRYPTOR.2.0.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/freeze.120.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/getshk.200.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/LHAExtractor.2.1.0.opt.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/LHAExtractor.2.1.0.src.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/NuFxMess.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/ProARC.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/PUPAAF.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/UnPP.1.1.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/Unzip.2.0.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/Unzip07.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/unzipiie-an 8-bit UnZip program in ShrinkIt format.shk",
        "ftp.apple.asimov.net/images/disk_utils/archivers/UnZip.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/UNZIP.P8.SHK",
        "ftp.apple.asimov.net/images/disk_utils/archivers/UNZIP41.SHK",
        "ftp.apple.asimov.net/images/disk_utils/prodos_file_nav/prodosfilenav3.shk",
    ];

    [Theory]
    [MemberData(nameof(DiskImages))]
    public void Ctor_Stream(string archiveName)
    {
        var filePath = Path.Combine("Samples", archiveName);
        using var stream = File.OpenRead(filePath);
        var archive = new ShrinkItArchive(stream);

        Debug.WriteLine($"Extracting archive: {archiveName}");
        Debug.WriteLine($"- Entries: {archive.Entries.Count}");
        Debug.WriteLine($"- Created: {archive.MasterHeaderBlock.CreationDate}");
        Debug.WriteLine($"- Modified: {archive.MasterHeaderBlock.LastModificationDate}");
        Debug.WriteLine($"- TotalSize: {archive.MasterHeaderBlock.TotalSize}");

        // Create output directory based on archive name (without extension)
        string archiveNameWithoutExtension = Path.GetFileNameWithoutExtension(archiveName);
        string outputDir = Path.Combine("Output", archiveNameWithoutExtension);

        // Delete the output directory if it exists
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        Directory.CreateDirectory(outputDir);

        for (int i = 0; i < archive.Entries.Count; i++)
        {
            var entry = archive.Entries[i];
            var fileName = archive.GetFileName(entry);
            if (string.IsNullOrEmpty(fileName))
            {
                throw new InvalidOperationException($"Entry {i} has an invalid or empty file name.");
            }

            string entryOutputPath = Path.Combine(outputDir, fileName);

            // Ensure the directory for the entry exists
            Directory.CreateDirectory(Path.GetDirectoryName(entryOutputPath)!);

            Debug.WriteLine($"- [{i}]: {fileName}");
            Debug.WriteLine($"    - AttributesCount: {entry.HeaderBlock.AttributesCount}");
            Debug.WriteLine($"    - VersionNumber: {entry.HeaderBlock.VersionNumber}");
            Debug.WriteLine($"    - TotalThreads: {entry.HeaderBlock.TotalThreads}");
            Debug.WriteLine($"    - FileSystemId: {entry.HeaderBlock.FileSystemId}");
            Debug.WriteLine($"    - FileSystemInfo: {entry.HeaderBlock.FileSystemInfo}");
            Debug.WriteLine($"    - AccessFlags: {entry.HeaderBlock.AccessFlags}");
            Debug.WriteLine($"    - FileType: 0x{entry.HeaderBlock.FileType:X4}");
            Debug.WriteLine($"    - AuxType: 0x{entry.HeaderBlock.AuxType:X4}");
            Debug.WriteLine($"    - StorageType: {entry.HeaderBlock.StorageType}");
            Debug.WriteLine($"    - CreationDate: {entry.HeaderBlock.CreationDate}");
            Debug.WriteLine($"    - LastModificationDate: {entry.HeaderBlock.LastModificationDate}");
            Debug.WriteLine($"    - ArchiveDate: {entry.HeaderBlock.ArchiveDate}");

            for (int j = 0; j < entry.Threads.Count; j++)
            {
                var thread = entry.Threads[j];
                Debug.WriteLine($"    - Thread [{j}]:");
                Debug.WriteLine($"        - Classification: {thread.Classification}");
                Debug.WriteLine($"        - Kind: {thread.Kind}");
                Debug.WriteLine($"        - Format: {thread.Format}");
                Debug.WriteLine($"        - UncompressedDataSize: {thread.UncompressedDataSize}");
                Debug.WriteLine($"        - CompressedDataSize: {thread.CompressedDataSize}");
            }

            if (archive.GetDataFork(entry) is byte[] dataFork)
            {
                File.WriteAllBytes(entryOutputPath, dataFork);
                Debug.WriteLine($"    - DataFork: {dataFork.Length} bytes written to {entryOutputPath}");
            }
            else
            {
                Debug.WriteLine($"    - DataFork: None");
            }
        }
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new ShrinkItArchive(null!));
    }
}

/// <summary>
/// SHA256 verification tests to ensure the LZW decompressor produces correct output.
/// These tests verify that decompressed data matches expected SHA256 hashes.
/// </summary>
public class Sha256VerificationTests
{
    private static string ComputeSha256(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies SHA256 hashes of files extracted from BSC-It.2.1.shk archive.
    /// This archive uses LZW/1 compression.
    /// </summary>
    [Theory]
    [InlineData("BscIT", "958db33d8c649b16692c7b4d095be59c78ca409f12b99bb5f2f434775045f245")]
    [InlineData("Docs.BscIT", "9c102274bdff078bdb2c9ea5102e8887edaccfd0a4248606de9dd31949ef182b")]
    public void BscIt_DataFork_MatchesSha256(string fileName, string expectedSha256)
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/BSC-It.2.1.shk";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        var entry = archive.Entries.FirstOrDefault(e => archive.GetFileName(e) == fileName);
        Assert.NotNull(entry);

        byte[]? dataFork = archive.GetDataFork(entry);
        Assert.NotNull(dataFork);

        string actualSha256 = ComputeSha256(dataFork);
        Assert.Equal(expectedSha256, actualSha256);
    }

    /// <summary>
    /// Verifies SHA256 hashes of files extracted from NuFxMess.SHK archive.
    /// This archive uses LZW/1 compression.
    /// </summary>
    [Theory]
    [InlineData("NUFX.1.0.1:NUFX.MESSENGER", "be9f3ebf3ae3414a2ed751efb68d2aa7160b39fef3b9a96ec4a2b764240d6b3c")]
    [InlineData("NUFX.1.0.1:NUFX.MESS.DOCS", "93df823afc1950e2fa247a7c42e6e2e127881d119efb842800ad1469aac34c77")]
    [InlineData("NUFX.1.0.1:NUFX.MESS.ICON", "2a4dcbc15ced87e1849c5862463f65e6e79b38a2ede07275d0fac427c5e5a622")]
    public void NuFxMess_DataFork_MatchesSha256(string fileName, string expectedSha256)
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/NuFxMess.SHK";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        var entry = archive.Entries.FirstOrDefault(e => archive.GetFileName(e) == fileName);
        Assert.NotNull(entry);

        byte[]? dataFork = archive.GetDataFork(entry);
        Assert.NotNull(dataFork);

        string actualSha256 = ComputeSha256(dataFork);
        Assert.Equal(expectedSha256, actualSha256);
    }

    /// <summary>
    /// Verifies SHA256 hashes of files extracted from getshk.200.shk archive.
    /// This archive uses LZW/2 compression and includes resource forks.
    /// </summary>
    [Theory]
    [InlineData("getshk2", "e6abd1a7725bafa27a1207466050c80b5b890789ab903457286a59ca3014ec4e")]
    [InlineData("readme.tch", "adcebeb2710cb61ed31c51b5da057a4579afe6de9a0fc08b31d2fd754f055faa")]
    [InlineData("readme.txt", "adcebeb2710cb61ed31c51b5da057a4579afe6de9a0fc08b31d2fd754f055faa")]
    public void GetShk_DataFork_MatchesSha256(string fileName, string expectedSha256)
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/getshk.200.shk";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        var entry = archive.Entries.FirstOrDefault(e => archive.GetFileName(e) == fileName);
        Assert.NotNull(entry);

        byte[]? dataFork = archive.GetDataFork(entry);
        Assert.NotNull(dataFork);

        string actualSha256 = ComputeSha256(dataFork);
        Assert.Equal(expectedSha256, actualSha256);
    }

    /// <summary>
    /// Verifies SHA256 hash of resource fork from getshk.200.shk archive.
    /// </summary>
    [Fact]
    public void GetShk_ResourceFork_MatchesSha256()
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/disk_utils/archivers/getshk.200.shk";
        const string expectedSha256 = "05b9bcd957dcbfaf975081a86dcf8d8e01bb633505f851b54f3093c4464339b7";

        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        var entry = archive.Entries.FirstOrDefault(e => archive.GetFileName(e) == "readme.tch");
        Assert.NotNull(entry);

        byte[]? resourceFork = archive.GetResourceFork(entry);
        Assert.NotNull(resourceFork);

        string actualSha256 = ComputeSha256(resourceFork);
        Assert.Equal(expectedSha256, actualSha256);
    }
}
