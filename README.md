# ShrinkItReader

A lightweight .NET library for reading ShrinkIt (NuFX) archive files (.shk, .sdk, .bxy). ShrinkIt was the most popular compression and archival tool for Apple II computers, supporting multiple compression formats and preserving file metadata across various Apple II file systems.

## Features

- Read ShrinkIt/NuFX archives (.shk, .sdk, .bxy files)
- Automatic detection and handling of Binary II wrapped archives
- Parse Master Header Block and archive entries
- Support for multiple compression formats:
  - Uncompressed
  - Dynamic LZW/1 and LZW/2 (ShrinkIt-specific)
- Extract data forks, resource forks, and disk images
- Preserve file metadata (file types, dates, access flags)
- Support for multiple file systems:
  - ProDOS/SOS
  - DOS 3.3/3.2
  - Apple II Pascal
  - Macintosh HFS/MFS
  - MS-DOS
  - CP/M
- Zero external dependencies (core library)
- Built for .NET 9.0

## Installation

Add the project reference to your .NET application:

```sh
dotnet add reference path/to/ShrinkItReader.csproj
```

Or, if published on NuGet:

```sh
dotnet add package ShrinkItReader
```

## Usage

### Opening a ShrinkIt Archive

```csharp
using ShrinkItReader;

// Open a ShrinkIt archive file
using var stream = File.OpenRead("archive.shk");

// Parse the archive
var archive = new ShrinkItArchive(stream);

// Get archive information
Console.WriteLine($"Total Records: {archive.MasterHeaderBlock.TotalRecords}");
Console.WriteLine($"Created: {archive.MasterHeaderBlock.CreationDate}");
Console.WriteLine($"Archive Size: {archive.MasterHeaderBlock.TotalSize} bytes");
```

### Listing Files in the Archive

```csharp
// Enumerate all entries in the archive
foreach (var entry in archive.Entries)
{
    var fileName = archive.GetFileName(entry);
    Console.WriteLine($"{fileName}");
    Console.WriteLine($"  File System: {entry.HeaderBlock.FileSystemId}");
    Console.WriteLine($"  Storage Type: {entry.HeaderBlock.StorageType}");
    Console.WriteLine($"  File Type: 0x{entry.HeaderBlock.FileType:X4}");
    Console.WriteLine($"  Created: {entry.HeaderBlock.CreationDate}");
    Console.WriteLine($"  Modified: {entry.HeaderBlock.LastModificationDate}");
    Console.WriteLine($"  Threads: {entry.Threads.Count}");
}
```

### Extracting File Data

```csharp
foreach (var entry in archive.Entries)
{
    var fileName = archive.GetFileName(entry);

    // Extract data fork (most common)
    var dataFork = archive.GetDataFork(entry);
    if (dataFork != null)
    {
        File.WriteAllBytes($"{fileName}", dataFork);
    }

    // Extract resource fork (Mac files)
    var resourceFork = archive.GetResourceFork(entry);
    if (resourceFork != null)
    {
        File.WriteAllBytes($"{fileName}.rsrc", resourceFork);
    }

    // Extract disk image
    var diskImage = archive.GetDiskImage(entry);
    if (diskImage != null)
    {
        File.WriteAllBytes($"{fileName}.dsk", diskImage);
    }
}
```

### Examining Thread Information

```csharp
foreach (var entry in archive.Entries)
{
    foreach (var thread in entry.Threads)
    {
        Console.WriteLine($"Thread Classification: {thread.Classification}");
        Console.WriteLine($"  Format: {thread.Format}");
        Console.WriteLine($"  Uncompressed Size: {thread.UncompressedDataSize}");
        Console.WriteLine($"  Compressed Size: {thread.CompressedDataSize}");

        var ratio = thread.CompressedDataSize > 0
            ? (1.0 - (double)thread.CompressedDataSize / thread.UncompressedDataSize) * 100
            : 0;
        Console.WriteLine($"  Compression Ratio: {ratio:F1}%");
    }
}
```

## API Overview

### ShrinkItArchive

The main class for reading ShrinkIt/NuFX archives.

- `ShrinkItArchive(Stream stream)` - Opens an archive from a stream
- `BinaryIIHeader` - Gets the Binary II header if the archive was wrapped (nullable)
- `MasterHeaderBlock` - Gets the Master Header Block with archive metadata
- `Entries` - Gets the list of archive entries
- `GetFileName(ShrinkItArchiveEntry)` - Gets the file name for an entry
- `GetDataFork(ShrinkItArchiveEntry)` - Extracts the data fork
- `GetResourceFork(ShrinkItArchiveEntry)` - Extracts the resource fork
- `GetDiskImage(ShrinkItArchiveEntry)` - Extracts the disk image

### ShrinkItMasterHeaderBlock

Contains archive-level metadata:

- `TotalRecords` - Number of entries in the archive
- `CreationDate` - When the archive was created
- `LastModificationDate` - When the archive was last modified
- `TotalSize` - Total size of the archive in bytes
- `VersionNumber` - NuFX format version
- `Crc` - CRC-16 checksum of the header

### ShrinkItArchiveEntry

Represents a single entry in the archive:

- `HeaderBlock` - File metadata (type, dates, permissions, etc.)
- `Threads` - List of data threads (forks, filenames, etc.)
- `FileName` - The file name (may be empty if stored in a thread)
- `DataOffset` - Offset to the data section
- `DataLength` - Total length of all thread data

### ShrinkItHeaderBlock

Contains file-level metadata:

- `FileSystemId` - Native file system (ProDOS, DOS 3.3, HFS, etc.)
- `FileType` - File type code (32-bit)
- `AuxType` - Auxiliary type code (32-bit)
- `StorageTypeOrBlockSize` - Raw storage type/block size value
- `StorageType` - Storage type (derived from low byte)
- `BlockSize` - Block size for disks (same as StorageTypeOrBlockSize)
- `AccessFlags` - File permissions (read, write, delete, etc.)
- `CreationDate` - File creation date
- `LastModificationDate` - File modification date
- `ArchiveDate` - When the file was added to the archive
- `FileSystemInfo` - File system separator character info
- `VersionNumber` - Record version number
- `AttributesCount` - Length of attribute section
- `TotalThreads` - Number of threads in this record

### ShrinkItThread

Describes a data thread within an entry:

- `Classification` - Type of data (Data, FileName, Message, Control)
- `Format` - Compression format (Uncompressed, DynamicLzw1, etc.)
- `Kind` - Specific kind within classification (0x0000 = data fork, 0x0001 = disk image, 0x0002 = resource fork)
- `UncompressedDataSize` - Size before compression
- `CompressedDataSize` - Size after compression
- `Crc` - CRC-16 checksum

### BinaryIIHeader

Contains Binary II wrapper metadata (present when archive is wrapped in Binary II format):

- `Version` - Binary II format version (0 or 1)
- `FileName` - Name of the wrapped file (max 64 characters)
- `FileType` - ProDOS file type
- `AuxType` - ProDOS auxiliary type
- `Access` - ProDOS access flags
- `StorageType` - ProDOS storage type
- `FileLength` - Length of the wrapped file in bytes
- `SizeInBlocks` - Size of file in 512-byte blocks
- `CreationDate` / `CreationTime` - File creation timestamp (ProDOS format)
- `ModificationDate` / `ModificationTime` - File modification timestamp (ProDOS format)
- `FilesToFollow` - Number of files to follow in archive
- `DiskSpaceNeeded` - Total disk space needed for all files
- `OperatingSystemType` - OS type identifier
- `NativeFileType` - Native file type (16-bit)
- `PhantomFileFlag` - Phantom file indicator
- `DataFlags` - Data flags (compressed/encrypted/sparse)
- `TotalEntrySize` - Total size including header and padded data

GS/OS extended attributes:
- `GsosAuxTypeHigh` - GS/OS auxiliary type high word
- `GsosAccessHigh` - GS/OS access high byte
- `GsosFileTypeHigh` - GS/OS file type high byte
- `GsosStorageTypeHigh` - GS/OS storage type high byte
- `GsosSizeInBlocksHigh` - GS/OS file size in blocks high word
- `GsosEofHigh` - GS/OS EOF high byte

### Enumerations

#### ShrinkItThreadFormat
- `Uncompressed` - No compression ✓
- `DynamicLzw1` - Dynamic LZW/1 (ShrinkIt-specific) ✓
- `DynamicLzw2` - Dynamic LZW/2 (ShrinkIt-specific) ✓
- `HuffmanSqueeze` - Huffman compression (not yet supported)
- `Unix12BitCompress` - Unix compress 12-bit (not yet supported)
- `Unix16BitCompress` - Unix compress 16-bit (not yet supported)

#### ShrinkItFileSystem
- `ProDOS_SOS` - ProDOS/SOS
- `DOS_3_3` - Apple DOS 3.3
- `DOS_3_2` - Apple DOS 3.2
- `AppleII_Pascal` - Apple II Pascal
- `Macintosh_HFS` - Macintosh HFS
- `Macintosh_MFS` - Macintosh MFS
- `MS_DOS` - MS-DOS
- `Apple_CP_M` - Apple CP/M
- And others...

#### ShrinkItStorageType
- `Standard1`, `Standard2`, `Standard3` - Standard file types
- `GSOSForkedFile` - GS/OS extended file
- `Subdirectory` - Directory entry

## Building

Build the project using the .NET SDK:

```sh
dotnet build
```

Run tests:

```sh
dotnet test
```

## ShrinkItDumper CLI

Extract files from ShrinkIt archives using the command-line dumper tool.

### Build

```sh
dotnet build dumper/ShrinkItDumper.csproj -c Release
```

### Usage

```sh
dotnet run --project dumper/ShrinkItDumper.csproj -- <input> [-o|--output <path>]
```

Or after building:

```sh
./dumper/bin/Release/net9.0/ShrinkItDumper <input> [-o|--output <path>]
```

**Arguments:**
- `<input>`: Path to the ShrinkIt archive file (.shk, .sdk, .bxy)
- `-o|--output`: Destination directory for extracted files (defaults to archive name)

**Output:**
- Data forks are saved with the original filename
- Disk images are saved with `.dsk` extension
- Resource forks are saved with `.rsrc` extension

**Example:**

```sh
dotnet run --project dumper/ShrinkItDumper.csproj -- archive.shk -o extracted_files
```

Output:
```
Archive: archive.shk
Total Records: 9
Output Directory: /path/to/extracted_files

Extracting: File.Nav.System
  File System: ProDOS_SOS
  Type: 0xB3  Aux: 0x0000  Storage: Standard3
  Created: 1992-03-15 14:30  Modified: 1992-03-15 14:30
  Data fork: /path/to/extracted_files/File.Nav.System (8.06 KB) [DynamicLzw2, 45.2% saved]
Extracting: File.Nav.Manual
  File System: ProDOS_SOS
  Type: 0x04  Aux: 0x0000  Storage: Standard2
  Created: 1992-03-10 09:15  Modified: 1992-03-10 09:15
  Data fork: /path/to/extracted_files/File.Nav.Manual (6.01 KB) [DynamicLzw2, 38.7% saved]
...

Extraction complete: /path/to/extracted_files
```

For Binary II wrapped archives (.bxy), additional header information is displayed:
```
Archive: archive.bxy
Binary II Wrapper: Yes (version 1)
  Wrapped File: archive.shk
  File Type: 0xE0
  Aux Type: 0x8002
Total Records: 11
Output Directory: /path/to/extracted_files
...
```

## Requirements

- .NET 9.0 or later

## License

MIT License. See [LICENSE](LICENSE) for details.

Copyright (c) 2025 Hugh Bellamy

## About ShrinkIt / NuFX

ShrinkIt was created by Andy Nicholas in 1986 and became the de facto standard for file compression and archival on Apple II computers. The NuFX (NuFile eXchange) format was designed to:

- Preserve file metadata across different file systems
- Support multiple compression algorithms
- Handle both files and disk images
- Maintain file forks (data and resource)
- Store creation and modification dates

**Format Characteristics:**
- Master Header Block with archive metadata
- Individual Header Blocks for each entry
- Thread-based structure for flexible data storage
- CRC-16 checksums for data integrity
- Support for multiple file systems (ProDOS, DOS 3.3, HFS, etc.)

**File Extensions:**
- `.shk` - ShrinkIt archive
- `.sdk` - ShrinkIt disk image archive
- `.bxy` - Binary II wrapped ShrinkIt archive

**Binary II Format:**
Binary II is a wrapper format developed by Gary B. Little to preserve ProDOS file attributes during file transfers. When ShrinkIt archives are wrapped in Binary II, they use the `.bxy` extension. This library automatically detects and unwraps Binary II headers, providing seamless access to the contained ShrinkIt archive.

## Related Projects

- [AppleDiskImageReader](https://github.com/hughbe/AppleDiskImageReader) - Reader for Apple II universal disk (.2mg) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 disk (.dsk) images
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS (.po) volumes
- [WozDiskImageReader](https://github.com/hughbe/WozDiskImageReader) - Reader for WOZ (.woz) disk images
- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes
- [ResourceForkReader](https://github.com/hughbe/ResourceForkReader) - Reader for Macintosh resource forks
- [StuffItReader](https://github.com/hughbe/StuffItReader) - Reader for StuffIt (.sit) archives

## Documentation

- [NuFX Archive Format Specification](https://nulib.com/library/FTN.e08002.htm)
- [Binary II Format Specification](https://ciderpress2.com/formatdoc/Binary2-notes.html)
- [ShrinkIt History and Documentation](https://en.wikipedia.org/wiki/ShrinkIt)
- [Apple II File Type Notes](https://www.apple2.org.za/gswv/a2zine/faqs/Csa2KFAQ.html)
