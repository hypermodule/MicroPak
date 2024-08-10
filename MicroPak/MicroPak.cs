using System.Security.Cryptography;
using System.Text;

namespace MicroPak;

public record InputFile(string Path, byte[] Data);

public enum EntryLocation
{
    Data,
    Index
}

public record Entry(ulong Offset, byte[] Data, byte[] Hash)
{
    private const uint Compression = 0;
    private const byte Flags = 0;
    private const uint CompressionBlockSize = 0;
    
    public Entry(InputFile file, long Offset) : this((ulong)Offset, file.Data, SHA1.HashData(file.Data))
    {
    }

    public void WriteTo(BinaryWriter writer, EntryLocation location)
    {
        writer.Write(location == EntryLocation.Data ? 0UL : Offset); // Offset
        writer.Write((ulong)Data.Length); // Compressed size
        writer.Write((ulong)Data.Length); // Uncompressed size
        writer.Write(Compression);
        writer.Write(Hash);
        writer.Write(Flags);
        writer.Write(CompressionBlockSize);
    }
}

public class Index(string mountPoint)
{
    private readonly SortedDictionary<string, Entry> _entries = new();

    public void AddEntry(string path, Entry entry) => _entries.Add(path, entry);

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.WriteString(mountPoint);

        writer.Write((uint)_entries.Count);

        foreach (var (path, entry) in _entries)
        {
            writer.WriteString(path);
            entry.WriteTo(writer, EntryLocation.Index);
        }

        return stream.ToArray();
    }
}

public static class MicroPak
{
    private const uint Magic = 0x5A6F12E1;
    private const string MountPoint = "../../../";
    private const byte Encrypted = 0;
    private const uint VersionMajor = 8;
    private const int AlgoSize = 5;

    public static byte[] PackFiles(IEnumerable<InputFile> files)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        var index = new Index(MountPoint);

        // Write entries and populate index
        foreach (var file in files.OrderBy(file => file.Path))
        {
            var entry = new Entry(file, writer.BaseStream.Position);
            entry.WriteTo(writer, EntryLocation.Data);
            writer.Write(file.Data);
            index.AddEntry(file.Path, entry);
        }

        // Write index
        var indexBytes = index.Serialize();
        var indexHash = SHA1.HashData(indexBytes);
        var indexOffset = writer.BaseStream.Position;
        writer.Write(indexBytes);

        // Write footer
        for (var i = 0; i < 16; i++) // Encryption guid
        {
            writer.Write((byte)0);
        }

        writer.Write(Encrypted);
        writer.Write(Magic);
        writer.Write(VersionMajor);
        writer.Write((ulong)indexOffset);
        writer.Write((ulong)indexBytes.Length);
        writer.Write(indexHash);
        for (var i = 0; i < AlgoSize; i++)
        {
            writer.Write(new byte[32]);
        }

        return stream.ToArray();
    }
}

public static class BinaryWriterExt
{
    private static bool IsAscii(string str) => str.All(c => c <= 127);

    public static void WriteString(this BinaryWriter writer, string str)
    {
        if (IsAscii(str))
        {
            var bytes = Encoding.ASCII.GetBytes(str);
            writer.Write((uint)(bytes.Length + 1));
            writer.Write(bytes);
            writer.Write((byte)0);
        }
        else
        {
            writer.Write(-(str.Length + 1));

            foreach (var c in str)
            {
                writer.Write((ushort)c);
            }

            writer.Write((ushort)0);
        }
    }
}