using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using ProDosVolumeReader.Resources;
using ProDosVolumeReader.Resources.Records;

namespace ShrinkItReader.Tests;

public class AppleIIDiskTests
{
    public static TheoryData<string> DiskImages =>
    [
        "ftp.apple.asimov.net/images/communications/AE.PRO.4.31P.SHK",
        "ftp.apple.asimov.net/images/communications/APPLENET.SHK",
        "ftp.apple.asimov.net/images/communications/BLACKSPRING.V3.SHK",
        "ftp.apple.asimov.net/images/communications/CommSystem.SHK",
        "ftp.apple.asimov.net/images/communications/ModemWorks.20B.DEMO.SHK",
        "ftp.apple.asimov.net/images/communications/PHREAK.AWAY.2.0.SHK",
        "ftp.apple.asimov.net/images/communications/PHREAK.AWAY.2.1.SHK",
        "ftp.apple.asimov.net/images/communications/SUPERTERM.SHK",
        "ftp.apple.asimov.net/images/communications/XFERKEEP.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/ACOS.2.01D5.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/ACOS.QUICKLOAD.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/ANET.DOC.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/Applenet20.shk",
        "ftp.apple.asimov.net/images/communications/bbs/CLASH.OF.ARMS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/FD.5.0.UPDATE.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/FD5.0.BBS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/FHA.PEPSI.V1.4C.BBS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/MAT.SEGS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/NETWORK.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/PAE.PRODOS.V4.5A.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/PRIME3.BBS.D1.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/PRIME3.BBS.D2.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/PRIME3.BBS.D3.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/TIC.DEMO.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/W6BBS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/WOF.SEGS.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/Warp6Upd3.0.SHK",
        "ftp.apple.asimov.net/images/communications/bbs/Warp6src2.5.BBS.shk",
        "ftp.apple.asimov.net/images/communications/bbs/fv4.0.tcq.shk",
        "ftp.apple.asimov.net/images/communications/bbs/fv4.1.b6.all.shk",
        "ftp.apple.asimov.net/images/communications/bbs/fvstuff.shk",
        "ftp.apple.asimov.net/images/communications/bbs/macos1.46.shk",
        "ftp.apple.asimov.net/images/communications/proterm/mouselink.shk",
        "ftp.apple.asimov.net/images/communications/proterm/mousetrap.shk",
        "ftp.apple.asimov.net/images/communications/proterm/mouslnk3.0.acos.shk",
        "ftp.apple.asimov.net/images/communications/proterm/streetscenes.shk",
        "ftp.apple.asimov.net/images/cpm/os/CPAM51A.SHK",
        "ftp.apple.asimov.net/images/cpm/os/CPAM51B.SHK",
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
        "ftp.apple.asimov.net/images/gs/productivity/TEACH1.1.1.SHK",
        "ftp.apple.asimov.net/images/gs/productivity/TIMECP2.1.SHK",
        "ftp.apple.asimov.net/images/gs/programming/BFCT.SHK",
        "ftp.apple.asimov.net/images/gs/programming/FINDER.S.SHK",
        "ftp.apple.asimov.net/images/gs/programming/lzss1.02.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/2SD402.BXY",
        "sembiance.com/fileFormatSamples/archive/nuFX/AGATE.SHK",
        "sembiance.com/fileFormatSamples/archive/nuFX/AP2GS.ARCH.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/HCIIGS_1.1-2of6.bxy",
        "sembiance.com/fileFormatSamples/archive/nuFX/IIGIF.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/NTGS.1.30.SHK",
        "sembiance.com/fileFormatSamples/archive/nuFX/SHRINKIT.SHK",
        "sembiance.com/fileFormatSamples/archive/nuFX/SRI.LANKA.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/TIMESIDED.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/apradio.shk",
        "sembiance.com/fileFormatSamples/archive/nuFX/star.trek.shk",
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

        Span<byte> buffer = stackalloc byte[4];
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
            Debug.WriteLine($"    - FileType: {entry.HeaderBlock.FileType} (0x{(byte)entry.HeaderBlock.FileType:X2})");
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

            if (archive.GetResourceFork(entry) is byte[] resourceForkBytes)
            {
                string resourceForkPath = entryOutputPath + ".rsrc";
                File.WriteAllBytes(resourceForkPath, resourceForkBytes);
                Debug.WriteLine($"    - ResourceFork: {resourceForkBytes.Length} bytes written to {resourceForkPath}");

                // Read the resource fork.
                var resourceForkStream = new MemoryStream(resourceForkBytes);
                resourceForkStream.Seek(0, SeekOrigin.Begin);

                if (resourceForkStream.Length == 0)
                {
                    Debug.WriteLine($"Skipping Resource Fork for {entry.FileName} as it is empty.");
                    return;
                }

                resourceForkStream.ReadExactly(buffer);
                uint version = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                if (version > 127)
                {
                    Debug.WriteLine($"Resource Fork {entry.FileName} has Mac format (version {version}), skipping parse.");
                    return;
                }

                resourceForkStream.Seek(0, SeekOrigin.Begin);

                GsOsResourceFork resourceFork;
                try
                {
                    resourceFork = new GsOsResourceFork(resourceForkStream);
                }
                catch (Exception ex) when (!(ex is FormatException) && !(ex is NotImplementedException))
                {
                    Debug.WriteLine($"Failed to parse Resource Fork for {entry.FileName}.");
                    throw;
                }

                Debug.WriteLine($"Successfully parsed Resource Fork for {entry.FileName} with {resourceFork.Map.ReferenceRecords.Length} resource index records.");

                foreach (var record in resourceFork.Map.ReferenceRecords)
                {
                    Debug.WriteLine($"  Resource Type: {record.Type}, ID: {record.ResourceID}, Offset: {record.DataOffset}, Size: {record.DataSize}, Attr: {record.Attributes}, PurgeLevel: {record.PurgeLevel}");

                    if (record.DataSize == 0)
                    {
                        Debug.WriteLine("    - Empty resource, skipping.");
                        continue;
                    }

                    // Export the binary data for the resource.
                    var resourceOutputPath = $"{entryOutputPath}.id_{record.ResourceID}";
                    Debug.WriteLine($"    Extracting Resource to {resourceOutputPath}");
                    byte[] resourceData = resourceFork.GetResourceData(record);
                    File.WriteAllBytes(resourceOutputPath, resourceData);

                    switch (record.Type)
                    {
                        case GsOsResourceForkType.Icon:
                        {
                            var iconRecord = new IconRecord(resourceData);
                            Debug.WriteLine($"    Icon Record: Width={iconRecord.Width}, Height={iconRecord.Height}, Size={iconRecord.Size} bytes");
                            break;
                        }
                   
                        case GsOsResourceForkType.ControlList:
                        {
                            var controlListRecord = new ControlListRecord(resourceData);
                            Debug.WriteLine($"    Control List Record: ControlCount={controlListRecord.Controls.Count}");
                            foreach (var control in controlListRecord.Controls)
                            {
                                Debug.WriteLine($"      Control: ID={control}");
                            }

                            break;
                        }

                        case GsOsResourceForkType.ControlTemplate:
                        {
                            var controlTemplateRecord = new ControlTemplateRecord(resourceData);
                            Debug.WriteLine($"    Control Template Record: ID={controlTemplateRecord.Header.ID}, Procedure={controlTemplateRecord.Header.Procedure}, Flags=0x{controlTemplateRecord.Header.Flags.Value:X4}, MoreFlags=0x{(int)controlTemplateRecord.Header.MoreFlags:X4}, ParameterCount={controlTemplateRecord.Header.ParameterCount}");
                            break;
                        }

                        case GsOsResourceForkType.PascalString:
                        {
                            var pascalStringRecord = new PascalStringRecord(resourceData);
                            Debug.WriteLine($"    Pascal String: '{pascalStringRecord.StringCharacters}'");
                            break;
                        }

                        case GsOsResourceForkType.ToolStartup:
                        {
                            var toolStartupRecord = new ToolStartupRecord(resourceData);
                            Debug.WriteLine($"    Tool Startup Record: Flags=0x{toolStartupRecord.Flags:X4}, VideoMode=0x{toolStartupRecord.VideoMode:X4}, ResourceFileID=0x{toolStartupRecord.ResourceFileID:X4}, PageHandle=0x{toolStartupRecord.PageHandle:X8}, NumberOfTools={toolStartupRecord.NumberOfTools}");
                            foreach (var tool in toolStartupRecord.Tools)
                            {
                                Debug.WriteLine($"      Tool: Number=0x{tool.ToolNumber:X4}, MinVersion=0x{tool.MinVersion:X4}");
                            }

                            break;
                        }

                        case GsOsResourceForkType.MenuBar:
                        {
                            var menuBarRecord = new MenuBarRecord(resourceData);
                            Debug.WriteLine($"    Menu Bar Record: Version={menuBarRecord.Version}, Flags=0x{menuBarRecord.Flags.RawValue:X4}, MenusReferenceCount={menuBarRecord.MenuReferences.Count}");
                            foreach (var menuRef in menuBarRecord.MenuReferences)
                            {
                                Debug.WriteLine($"      Menu Reference: 0x{menuRef:X8}");
                            }

                            break;
                        }

                        case GsOsResourceForkType.Menu:
                        {
                            var menuRecord = new MenuRecord(resourceData);
                            Debug.WriteLine($"    Menu Record: ID={menuRecord.ID}, Version={menuRecord.Version}, Flags=0x{menuRecord.Flags.Value:X4}, TitleRef={menuRecord.TitleReference}, ItemReferences={menuRecord.ItemReferences.Count}");
                            foreach (var itemRef in menuRecord.ItemReferences)
                            {
                                Debug.WriteLine($"      Item Reference: 0x{itemRef:X8}");
                            }

                            break;
                        }

                        case GsOsResourceForkType.MenuItem:
                        {
                            var menuItemRecord = new MenuItemRecord(resourceData);
                            Debug.WriteLine($"    Menu Item Record: ID={menuItemRecord.MenuItemID}, Version={menuItemRecord.Version}, PrimaryKeystroke=0x{menuItemRecord.PrimaryKeystrokeEquivalentCharacter:X2}, AlternateKeystroke=0x{menuItemRecord.AlternateKeystrokeEquivalentCharacter:X2}, CheckmarkChar='{menuItemRecord.ItemCheckmarkCharacter}', Flags=0x{menuItemRecord.Flags.Value:X4}, TitleRef={menuItemRecord.TitleReference}");
                            break;
                        }

                        case GsOsResourceForkType.WindowParam1:
                        {
                            var windowParam1Record = new WindowParam1Record(resourceData);
                            Debug.WriteLine($"    Window Param1 Record: Length={windowParam1Record.Length} bytes, Frame=0x{windowParam1Record.Frame:X2} TitleReference={windowParam1Record.TitleReference}, InfoTextHeight={windowParam1Record.InfoTextHeight}, FrameDefinitionProcedure=0x{windowParam1Record.FrameDefinitionProcedure:X8}, InfoTextDefinitionProcedure=0x{windowParam1Record.InfoTextDefinitionProcedure:X8}, ContentDefinitionProcedure=0x{windowParam1Record.ContentDefinitionProcedure:X8}, Position=({windowParam1Record.Position.Left},{windowParam1Record.Position.Top},{windowParam1Record.Position.Right},{windowParam1Record.Position.Bottom}), Plane={windowParam1Record.Plane}, ControlTemplateReference={windowParam1Record.ControlTemplateReference}, ReferenceTypes=0x{windowParam1Record.ReferenceTypes.RawValue:X4}");
                            break;
                        }

                        case GsOsResourceForkType.TextEditStyle:
                        {
                            var textStyleRecord = new TextStyleRecord(resourceData);
                            Debug.WriteLine($"    Text Style Record: TEFormat Version={textStyleRecord.Format.Version}, RulerCount={textStyleRecord.Format.Rulers.Count}, StyleCount={textStyleRecord.Format.Styles.Count}, StyleItemCount={textStyleRecord.Format.NumberOfStyles}");
                            foreach (var ruler in textStyleRecord.Format.Rulers)
                            {
                                Debug.WriteLine($"      Ruler: LeftMargin={ruler.LeftMargin}, LeftIndent={ruler.LeftIndent}, RightMargin={ruler.RightMargin}, Justification={ruler.TabType}, TabStopCount={ruler.TabStops?.Count ?? 0}, TabTerminator={ruler.TabTerminator}");
                                foreach (var tabStop in ruler.TabStops ?? Enumerable.Empty<ushort>())
                                {
                                    Debug.WriteLine($"        Tab Stop: {tabStop}");
                                }
                            }
                            foreach (var style in textStyleRecord.Format.Styles)
                            {
                                Debug.WriteLine($"      Style: FontID={style.FontID}, ForegroundColor=0x{style.ForegroundColor:X4}, BackgroundColor=0x{style.BackgroundColor:X4}, UserData=0x{style.UserData:X8}");
                            }
                            foreach (var styleItem in textStyleRecord.Format.StyleItems)
                            {
                                Debug.WriteLine($"      Style Item: Length={styleItem.Length}, Offset={styleItem.Offset}");
                            }

                            break;
                        }

                        case GsOsResourceForkType.TextForLETextBox2:
                        {
                            var textBox2Record = new TextForLETextBox2Record(resourceData);
                            Debug.WriteLine($"    Text Box 2 Record: Length={textBox2Record.Length} bytes");
                            break;
                        }

                        case GsOsResourceForkType.AlertString:
                        {
                            var alertStringRecord = new AlertStringRecord(resourceData);
                            Debug.WriteLine($"    Alert String Record: '{alertStringRecord.Message}'");
                            break;
                        }
                        
                        case GsOsResourceForkType.CDEVCode:
                        {
                            var codeRecord = new CodeRecord(resourceData);
                            Debug.WriteLine($"    CDEV Code Record: CodeLength={codeRecord.Data.Length}");
                            break;
                        }

                        case GsOsResourceForkType.CDEVFlags:
                        {
                            var cdevFlagsRecord = new CDEVFlagsRecord(resourceData);
                            Debug.WriteLine($"    CDEV Flags Record: Flags=0x{cdevFlagsRecord.Flags:X4}, Enabled={cdevFlagsRecord.Enabled}, Version={cdevFlagsRecord.Version}, Machine={cdevFlagsRecord.Machine}, Reserved={cdevFlagsRecord.Reserved}, DataRectangle={cdevFlagsRecord.DataRectangle}, CDEVName='{cdevFlagsRecord.CDEVName}', AuthorName='{cdevFlagsRecord.AuthorName}', VersionName='{cdevFlagsRecord.VersionName}'");
                            break;
                        }

                        case GsOsResourceForkType.ErrorString:
                        {
                            var errorStringRecord = new ErrorStringRecord(resourceData);
                            Debug.WriteLine($"    Error String Record: '{errorStringRecord.Message}'");
                            break;
                        }

                        case GsOsResourceForkType.Version:
                        {
                            var versionRecord = new VersionRecord(resourceData);
                            Debug.WriteLine($"    Version Record: Version={versionRecord.Version}, Region={versionRecord.Region}, Name='{versionRecord.Name}', MoreInfo='{versionRecord.MoreInfo}'");
                            break;
                        }

                        case GsOsResourceForkType.Comment:
                        {
                            var commentRecord = new CommentRecord(resourceData);
                            Debug.WriteLine($"    Comment Record: '{commentRecord.Comment}'");
                            break;
                        }

                        case (GsOsResourceForkType)0x6FFE:
                        case (GsOsResourceForkType)0x6FFF:
                        case (GsOsResourceForkType)0x7001:
                        {
                            // Unknown.
                            break;
                        }

                        default:
                        {
                            if (Enum.IsDefined(record.Type))
                            {
                                throw new NotImplementedException($"Resource Type {record.Type} not supported.");
                            }
                            else
                            {
                                throw new NotImplementedException($"Resource Type 0x{(ushort)record.Type:X4} not supported.");
                            }
                        }
                    }
                }

            }
            else
            {
                Debug.WriteLine($"    - ResourceFork: None");
            }
        }
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new ShrinkItArchive(null!));
    }

    /// <summary>
    /// Verifies that the FV.BBS.SHK archive cannot be fully read because it is corrupt.
    /// The archive can be opened but several entries have corrupted LZW/2 data
    /// that causes InvalidOperationException when extracting.
    /// </summary>
    [Fact]
    public void FvBbs_CorruptArchive_ThrowsInvalidOperationExceptionOnExtract()
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/communications/bbs/FV.BBS.SHK";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        // Verify we have the expected number of entries
        Assert.Equal(13, archive.Entries.Count);

        // Track which entries are corrupt
        var corruptEntries = new List<string>();
        var validEntries = new List<string>();

        foreach (var entry in archive.Entries)
        {
            var fileName = archive.GetFileName(entry);
            try
            {
                var dataFork = archive.GetDataFork(entry);
                validEntries.Add(fileName);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("LZW/2 input length mismatch"))
            {
                corruptEntries.Add(fileName);
            }
        }

        // The first corrupt entry is "FV:Belief.bsq"
        Assert.Contains("FV:Belief.bsq", corruptEntries);
        
        // There should be at least one corrupt entry
        Assert.NotEmpty(corruptEntries);
    }

    /// <summary>
    /// Verifies that the PAE.PRODOS.V4.5A.SHK archive has the correct dates,
    /// which tests the minute overflow handling in ShrinkItDateTime.
    /// The archive has minutes > 59 that should overflow into hours.
    /// </summary>
    [Fact]
    public void PaeProDos_Dates_AreCorrect()
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/communications/bbs/PAE.PRODOS.V4.5A.SHK";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        // Verify MasterHeaderBlock dates
        Assert.Equal(new DateTime(1990, 4, 28, 18, 12, 0), archive.MasterHeaderBlock.CreationDate.ToDateTime());
        Assert.Equal(new DateTime(1990, 4, 28, 18, 12, 0), archive.MasterHeaderBlock.LastModificationDate.ToDateTime());

        // Verify entry dates (CreationDate, LastModificationDate, ArchiveDate)
        var expectedDates = new (string FileName, DateTime Created, DateTime Modified, DateTime Archived)[]
        {
            ("HELP.PROTOCOL", new(1988, 7, 6, 19, 11, 0), new(1988, 7, 6, 19, 11, 0), new(1990, 4, 28, 18, 10, 0)),
            ("PAE.COMMANDS.S", new(1988, 12, 17, 13, 21, 0), new(1988, 12, 17, 15, 29, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.CONFIGURE.S", new(1989, 1, 21, 13, 52, 0), new(1989, 1, 21, 13, 52, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.DOX", new(1989, 1, 21, 13, 59, 0), new(1989, 1, 21, 13, 59, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.FILETYPES", new(1988, 3, 27, 21, 42, 0), new(1988, 3, 27, 21, 52, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.FTYPES", new(1988, 5, 21, 12, 43, 0), new(1988, 5, 21, 12, 43, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.GOODBYE", new(1988, 5, 13, 23, 51, 0), new(1988, 5, 13, 23, 51, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.MENU", new(1988, 11, 6, 15, 16, 0), new(1988, 11, 6, 15, 16, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.PDOS.MAIN.S", new(1988, 12, 19, 10, 23, 0), new(1988, 12, 19, 12, 31, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.SYSOP", new(1988, 5, 26, 21, 53, 0), new(1988, 5, 26, 21, 53, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.SYSOP.S", new(1988, 12, 17, 13, 30, 0), new(1988, 12, 17, 15, 38, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.V4.5.FIX.S", new(1988, 10, 21, 8, 10, 0), new(1988, 10, 21, 10, 18, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.WELCOME", new(1988, 5, 13, 14, 26, 0), new(1988, 5, 13, 14, 26, 0), new(1990, 4, 28, 18, 11, 0)),
            ("PAE.XFERS.S", new(1989, 1, 7, 19, 55, 0), new(1989, 1, 7, 22, 3, 0), new(1990, 4, 28, 18, 12, 0)),
            ("SYSOP.MENU.DOX", new(1988, 6, 5, 18, 32, 0), new(1988, 6, 5, 18, 32, 0), new(1990, 4, 28, 18, 12, 0)),
            ("PROTOCOL.DOWN", new(1988, 4, 20, 1, 28, 0), new(1988, 4, 20, 1, 42, 0), new(1990, 4, 28, 18, 12, 0)),
            ("PROTOCOL.UP", new(1988, 4, 20, 1, 27, 0), new(1988, 4, 20, 1, 27, 0), new(1990, 4, 28, 18, 12, 0)),
            ("X.DN", new(1988, 2, 1, 19, 29, 0), new(1987, 4, 26, 19, 2, 0), new(1990, 4, 28, 18, 12, 0)),
            ("X.UP", new(1988, 2, 1, 19, 29, 0), new(1987, 4, 26, 19, 3, 0), new(1990, 4, 28, 18, 12, 0)),
            ("XDOS", new(1988, 4, 19, 23, 37, 0), new(1988, 4, 4, 1, 8, 0), new(1990, 4, 28, 18, 12, 0)),
            ("XSHOW", new(1988, 8, 15, 11, 53, 0), new(1988, 8, 15, 11, 52, 0), new(1990, 4, 28, 18, 12, 0)),
        };

        Assert.Equal(expectedDates.Length, archive.Entries.Count);

        for (int i = 0; i < expectedDates.Length; i++)
        {
            var (fileName, created, modified, archived) = expectedDates[i];
            var entry = archive.Entries[i];
            var actualFileName = archive.GetFileName(entry);

            Assert.Equal(fileName, actualFileName);
            Assert.Equal(created, entry.HeaderBlock.CreationDate.ToDateTime());
            Assert.Equal(modified, entry.HeaderBlock.LastModificationDate.ToDateTime());
            Assert.Equal(archived, entry.HeaderBlock.ArchiveDate.ToDateTime());
        }
    }

    /// <summary>
    /// Verifies that the mousetrap.shk archive has the correct dates,
    /// which tests the hour overflow handling in ShrinkItDateTime.
    /// The archive has hours > 23 that should overflow into days.
    /// </summary>
    [Fact]
    public void Mousetrap_Dates_AreCorrect()
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/communications/proterm/mousetrap.shk";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        // Verify MasterHeaderBlock dates
        Assert.Equal(new DateTime(2017, 1, 11, 0, 7, 0), archive.MasterHeaderBlock.CreationDate.ToDateTime());
        Assert.Equal(new DateTime(2017, 1, 11, 20, 8, 54), archive.MasterHeaderBlock.LastModificationDate.ToDateTime());

        // Verify entry dates (CreationDate, LastModificationDate, ArchiveDate)
        var expectedDates = new (string FileName, DateTime Created, DateTime Modified, DateTime Archived)[]
        {
            ("MT.SYSTEM", new(1989, 2, 7, 12, 33, 0), new(1987, 10, 7, 13, 28, 0), new(2017, 1, 11, 0, 6, 0)),
            ("MT.START", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 15, 56, 0), new(2017, 1, 11, 0, 6, 0)),
            ("MT.HELP", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 16, 26, 0), new(2017, 1, 11, 0, 6, 0)),
            ("MT.MAIN", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 15, 58, 0), new(2017, 1, 11, 0, 6, 0)),
            ("MT.INTRO", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 17, 3, 0), new(2017, 1, 11, 0, 6, 0)),
            ("DESKTOP", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 16, 51, 0), new(2017, 1, 11, 0, 6, 0)),
            ("UPLOADING", new(1990, 8, 27, 8, 47, 0), new(1990, 8, 27, 8, 47, 0), new(2017, 1, 11, 0, 7, 0)),
            ("IDEAS.80", new(1990, 8, 10, 15, 15, 0), new(1990, 8, 10, 15, 15, 0), new(2017, 1, 11, 0, 7, 0)),
            ("FRAME", new(1989, 2, 7, 12, 33, 0), new(1988, 6, 30, 23, 53, 0), new(2017, 1, 11, 0, 7, 0)),
            ("GOLDMINE", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 16, 36, 0), new(2017, 1, 11, 0, 7, 0)),
            ("ALTERNATIVE", new(1989, 2, 7, 12, 33, 0), new(1988, 12, 6, 16, 37, 0), new(2017, 1, 11, 0, 7, 0)),
            ("YANK.ADD", new(1989, 3, 4, 14, 4, 0), new(1989, 3, 4, 14, 4, 0), new(2017, 1, 11, 0, 7, 0)),
            ("MT.DEFAULTS", new(1989, 3, 6, 18, 32, 0), new(1988, 8, 21, 17, 29, 0), new(2017, 1, 11, 0, 7, 0)),
            ("SAMPLE.PROGRAM", new(1989, 3, 6, 18, 32, 0), new(1988, 8, 20, 22, 28, 0), new(2017, 1, 11, 0, 7, 0)),
            ("PRINT.MOUSETEXT", new(1989, 3, 6, 18, 32, 0), new(1988, 8, 22, 0, 31, 0), new(2017, 1, 11, 0, 7, 0)),
            ("BAR.ADD", new(1989, 7, 2, 0, 0, 0), new(1989, 7, 2, 0, 0, 0), new(2017, 1, 11, 0, 7, 0)),
            ("MENU.PT", new(2000, 1, 1, 0, 0, 0), new(2000, 1, 1, 0, 0, 0), new(2017, 1, 11, 0, 7, 0)),
            ("PSE.ADD", new(1989, 7, 2, 0, 0, 0), new(1989, 7, 2, 0, 0, 0), new(2017, 1, 11, 0, 7, 0)),
            ("MAIN.MENU", new(1989, 8, 16, 15, 0, 0), new(1989, 8, 16, 15, 0, 0), new(2017, 1, 11, 0, 7, 0)),
            ("PRODOS", new(1990, 8, 8, 19, 24, 0), new(1989, 6, 14, 9, 56, 0), new(2017, 1, 11, 0, 7, 0)),
            ("TLOC.ADD", new(1990, 9, 22, 20, 16, 0), new(1990, 9, 22, 20, 16, 0), new(2017, 1, 11, 0, 7, 0)),
            ("AUX", new(1990, 8, 27, 13, 8, 0), new(1990, 8, 27, 13, 16, 0), new(2017, 1, 11, 0, 7, 0)),
            ("CON", new(1990, 8, 27, 13, 13, 0), new(1990, 8, 27, 13, 13, 0), new(2017, 1, 11, 0, 7, 0)),
            ("MAP", new(1998, 1, 7, 22, 40, 0), new(1998, 1, 7, 22, 40, 0), new(2017, 1, 11, 0, 7, 0)),
            ("CRAP", new(1998, 1, 12, 21, 39, 0), new(1998, 1, 12, 21, 39, 0), new(2017, 1, 11, 0, 7, 0)),
        };

        Assert.Equal(expectedDates.Length, archive.Entries.Count);

        for (int i = 0; i < expectedDates.Length; i++)
        {
            var (fileName, created, modified, archived) = expectedDates[i];
            var entry = archive.Entries[i];
            var actualFileName = archive.GetFileName(entry);

            Assert.Equal(fileName, actualFileName);
            Assert.Equal(created, entry.HeaderBlock.CreationDate.ToDateTime());
            Assert.Equal(modified, entry.HeaderBlock.LastModificationDate.ToDateTime());
            Assert.Equal(archived, entry.HeaderBlock.ArchiveDate.ToDateTime());
        }
    }

    /// <summary>
    /// Verifies that the stream-based extraction produces the same results as the byte array extraction.
    /// </summary>
    [Theory]
    [MemberData(nameof(DiskImages))]
    public void ExtractDataForkTo_MatchesGetDataFork(string archiveName)
    {
        var filePath = Path.Combine("Samples", archiveName);
        using var stream = File.OpenRead(filePath);
        var archive = new ShrinkItArchive(stream);

        foreach (var entry in archive.Entries)
        {
            // Get the data fork using the byte[] API
            var dataForkBytes = archive.GetDataFork(entry);
            
            // Get the data fork using the stream API
            using var memoryStream = new MemoryStream();
            var hasDataFork = archive.ExtractDataForkTo(entry, memoryStream);

            if (dataForkBytes == null)
            {
                Assert.False(hasDataFork, $"ExtractDataForkTo should return false when GetDataFork returns null for entry {archive.GetFileName(entry)}");
            }
            else
            {
                Assert.True(hasDataFork, $"ExtractDataForkTo should return true when GetDataFork returns data for entry {archive.GetFileName(entry)}");
                Assert.Equal(dataForkBytes, memoryStream.ToArray());
            }
        }
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

    /// <summary>
    /// Verifies SHA256 hashes of files extracted from PAE.PRODOS.V4.5A.SHK archive.
    /// This archive has minute overflow values that need to be handled correctly.
    /// </summary>
    [Theory]
    [InlineData("HELP.PROTOCOL", "1157311ad5a232d50f2c87ddce66257e7fc7648afeb9c97b04c3f02c81f1df50")]
    [InlineData("PAE.COMMANDS.S", "33f6fcd0b63f8da43010ce41213715bed53487ffbce4e3c79d238d427a6fab7c")]
    [InlineData("PAE.CONFIGURE.S", "18d28615637ed466849fa27b16de78a18659606d854842f58397556842e981b2")]
    [InlineData("PAE.DOX", "93cba802007f55cf2d93c914820e529da7bbfe021203d753a47f4e7eb47bf56d")]
    [InlineData("PAE.FILETYPES", "da68aeee5757611419a1ab85948ad18aae8da07164ad40bc95abe3c4f04bbda6")]
    [InlineData("PAE.FTYPES", "60003001fb7f551379307c48e8316fb1f8acb4e88b668657c1b877ddf0621ddd")]
    [InlineData("PAE.GOODBYE", "c7e4825cab04cb9ee189891f0793ea5b0ec9f8ff30bcb349d20c8a00e6864912")]
    [InlineData("PAE.MENU", "5bf08d7701961ea61c93a96b4723d432059e0f884fc977652b6343579df0c5c4")]
    [InlineData("PAE.PDOS.MAIN.S", "7b27addc51d51a78e371906fa11bbe099de411c10598f64b77cc059aecc27451")]
    [InlineData("PAE.SYSOP", "2c6515df9bfed57da8d11525f8529834d577d4fd338699cf8e042dadc802d7c1")]
    [InlineData("PAE.SYSOP.S", "08cbe34e7e6c92506e230eb7ed4e0d1a5c773270f46e37ae6cadef914cc5ff74")]
    [InlineData("PAE.V4.5.FIX.S", "7a896f740d8bdc7c1fc47d55875a823bbaf4c7bbb368ca7d6fb33bfdc2758ed0")]
    [InlineData("PAE.WELCOME", "552d0e70f8fdb515b388979a4dfd445f3a50a391f87e63adc0ef18eae166662f")]
    [InlineData("PAE.XFERS.S", "73367b9addd4e6b792820926ed5a2bd8b42c5dab495fa9dd5819a8efe05a1228")]
    [InlineData("PROTOCOL.DOWN", "7d3ddeda7d4fede2cf239c265b4fe56681823ac71473ff89e7b2e489a7eab07d")]
    [InlineData("PROTOCOL.UP", "b7da81533016fbffa296abaa4d9883d14b05875756e441bd7f3f5c18ea99b396")]
    [InlineData("SYSOP.MENU.DOX", "6263f4c9f5f12ee21b0b85bfada59fb15c92cb4c78e511b16b9af7e95158e34d")]
    [InlineData("X.DN", "105e42232af983563fc8610370bf5f9e10d526d7bb68d292e100d66c13b8e767")]
    [InlineData("X.UP", "3c92ac77bfd21214c32828737dd2fa3508b738a6b85f0c82712a7e9c23868d4c")]
    [InlineData("XDOS", "f053e45456d09690ac84ae2f9014f8b1108a0109c31a68773ada299a95bf8b80")]
    [InlineData("XSHOW", "1b8d8388f5c3c69ce33f009cf455ac64c1b36f2e7cea91a1a08502e7e2af4191")]
    public void PaeProDos_DataFork_MatchesSha256(string fileName, string expectedSha256)
    {
        const string archivePath = "Samples/ftp.apple.asimov.net/images/communications/bbs/PAE.PRODOS.V4.5A.SHK";
        using var stream = File.OpenRead(archivePath);
        var archive = new ShrinkItArchive(stream);

        var entry = archive.Entries.FirstOrDefault(e => archive.GetFileName(e) == fileName);
        Assert.NotNull(entry);

        byte[]? dataFork = archive.GetDataFork(entry);
        Assert.NotNull(dataFork);

        string actualSha256 = ComputeSha256(dataFork);
        Assert.Equal(expectedSha256, actualSha256);
    }
}
