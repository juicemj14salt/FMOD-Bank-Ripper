// FmodBankRipper.Core/FsbScanner.cs
using System.Text;

namespace FmodBankRipper.Core;

public static class FsbScanner
{
    private static readonly byte[] Fsb5Magic = "FSB5"u8.ToArray();
    private static readonly byte[] Fsb4Magic = "FSB4"u8.ToArray();
    private static readonly byte[] Fsb3Magic = "FSB3"u8.ToArray();

    public static List<FsbChunk> ExtractChunks(byte[] data)
    {
        var chunks = new List<FsbChunk>();

        chunks.AddRange(FindChunks(data, Fsb5Magic, FsbVersion.Fsb5));
        chunks.AddRange(FindChunks(data, Fsb4Magic, FsbVersion.Fsb4));
        chunks.AddRange(FindChunks(data, Fsb3Magic, FsbVersion.Fsb3));

        return chunks;
    }

    private static List<FsbChunk> FindChunks(byte[] data, byte[] magic, FsbVersion version)
    {
        var chunks = new List<FsbChunk>();
        int offset = 0;

        while (offset < data.Length - 4)
        {
            int index = IndexOf(data, magic, offset);
            if (index == -1) break;

            uint? totalSize = version switch
            {
                FsbVersion.Fsb5 => ParseFsb5Size(data, index),
                FsbVersion.Fsb4 => ParseFsb4Size(data, index),
                FsbVersion.Fsb3 => ParseFsb3Size(data, index),
                _ => null
            };

            // Fallback: if we can't calculate size, just read from magic to EOF
            if (!totalSize.HasValue || totalSize.Value == 0 || totalSize.Value > data.Length - index)
            {
                totalSize = (uint)(data.Length - index);
            }

            var chunk = new byte[totalSize.Value];
            Buffer.BlockCopy(data, index, chunk, 0, (int)totalSize.Value);
            chunks.Add(new FsbChunk(version, chunk));

            offset = index + (int)totalSize.Value;
        }

        return chunks;
    }

    private static uint? ParseFsb5Size(byte[] data, int index)
    {
        // FSB5 Header Layout (0x30 = 48 bytes):
        // 0x00: "FSB5"
        // 0x04: version
        // 0x08: numSamples
        // 0x0C: sampleHeaderSize  <-- NOT total size!
        // 0x10: sampleNamesSize
        // 0x14: sampleDataSize
        // 0x18: flags
        // 0x1C: bankHash (8 bytes)
        // 0x24: dummy0
        // 0x28: dummy1
        // 0x2C: dummy2
        if (index + 0x18 > data.Length) return null;

        uint sampleHeaderSize = BitConverter.ToUInt32(data, index + 0x0C);
        uint sampleNamesSize = BitConverter.ToUInt32(data, index + 0x10);
        uint sampleDataSize = BitConverter.ToUInt32(data, index + 0x14);

        // Total = header + headers table + names table + data
        // Some FSB5 files have 0x40 header, but 0x30 is standard. Add padding safety.
        uint total = 0x30 + sampleHeaderSize + sampleNamesSize + sampleDataSize;

        // Align to 32-byte boundary (common in FSB files)
        total = (total + 0x1F) & ~0x1FU;

        return total;
    }

    private static uint? ParseFsb4Size(byte[] data, int index)
    {
        // FSB4 Header Layout (0x30 = 48 bytes):
        // 0x00: "FSB4"
        // 0x04: version
        // 0x08: numSamples
        // 0x0C: sampleHeaderSize
        // 0x10: sampleNamesSize
        // 0x14: sampleDataSize
        // 0x18: flags
        // 0x1C: bankHash (8 bytes)
        // 0x24: dummy0
        // 0x28: dummy1
        // 0x2C: dummy2
        if (index + 0x18 > data.Length) return null;

        uint sampleHeaderSize = BitConverter.ToUInt32(data, index + 0x0C);
        uint sampleNamesSize = BitConverter.ToUInt32(data, index + 0x10);
        uint sampleDataSize = BitConverter.ToUInt32(data, index + 0x14);

        uint total = 0x30 + sampleHeaderSize + sampleNamesSize + sampleDataSize;
        total = (total + 0x1F) & ~0x1FU;

        return total;
    }

    private static uint? ParseFsb3Size(byte[] data, int index)
    {
        // FSB3 Header Layout (0x18 = 24 bytes):
        // 0x00: "FSB3"
        // 0x04: version
        // 0x08: numSamples
        // 0x0C: sampleHeaderSize
        // 0x10: sampleNamesSize
        // 0x14: sampleDataSize
        if (index + 0x18 > data.Length) return null;

        uint sampleHeaderSize = BitConverter.ToUInt32(data, index + 0x0C);
        uint sampleNamesSize = BitConverter.ToUInt32(data, index + 0x10);
        uint sampleDataSize = BitConverter.ToUInt32(data, index + 0x14);

        uint total = 0x18 + sampleHeaderSize + sampleNamesSize + sampleDataSize;
        total = (total + 0x1F) & ~0x1FU;

        return total;
    }

    private static int IndexOf(byte[] data, byte[] pattern, int start)
    {
        for (int i = start; i <= data.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
                if (data[i + j] != pattern[j]) { match = false; break; }
            if (match) return i;
        }
        return -1;
    }
}

public enum FsbVersion
{
    Fsb3,
    Fsb4,
    Fsb5
}

public sealed class FsbChunk
{
    public FsbVersion Version { get; }
    public byte[] Data { get; }

    public FsbChunk(FsbVersion version, byte[] data)
    {
        Version = version;
        Data = data;
    }
}