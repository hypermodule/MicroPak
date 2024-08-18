using System.Security.Cryptography;
using System.Text;

namespace MicroPak;

public static class Pak
{
    private static void WriteEntry(BinaryWriter writer, ulong offset, ulong size, byte[] hash)
    {
        const uint compression = 0;
        const byte flags = 0;
        const uint compressionBlockSize = 0;

        writer.Write(offset);
        writer.Write(size); // Compressed size
        writer.Write(size); // Uncompressed size
        writer.Write(compression);
        writer.Write(hash);
        writer.Write(flags);
        writer.Write(compressionBlockSize);
    }

    /// <summary>
    /// Makes a .pak file out of the specified files.
    /// </summary>
    /// <param name="files">A dictionary of (path, bytes) entries.</param>
    /// <example>
    /// <code>
    /// Pak.Create(new Dictionary&lt;string, byte[]&gt;
    /// {
    ///     ["CoolGame/Content/Dir1/Dir2/FileA.txt"] = "Hello, world!"u8.ToArray(),
    ///     ["CoolGame/Content/Dir1/Dir2/FileB.txt"] = "Goodbye, world!"u8.ToArray()
    /// });
    /// </code>
    /// </example>
    /// <returns>The .pak file represented as a byte array.</returns>
    public static byte[] Create(IDictionary<string, byte[]> files)
    {
        const uint magic = 0x5A6F12E1;
        const string mountPoint = "../../../";
        const byte encrypted = 0;
        const uint versionMajor = 8;
        const int algoSize = 5;

        // Prepare files
        var sortedFiles = files.Select(file => new
            {
                Path = file.Key.Replace('\\', '/'),
                Data = file.Value,
                Hash = SHA1.HashData(file.Value)
            })
            .OrderBy(x => x.Path)
            .ToList();
        
        // Prepare for writing
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var offsets = new List<ulong>();

        // Write entries
        foreach (var file in sortedFiles)
        {
            offsets.Add((ulong)writer.BaseStream.Position);
            WriteEntry(writer, 0UL, (ulong)file.Data.Length, file.Hash);
            writer.Write(file.Data);
        }

        // Write index
        var indexOffset = writer.BaseStream.Position;
        writer.WriteFString(mountPoint);
        writer.Write((uint)files.Count);
        foreach (var (file, offset) in sortedFiles.Zip(offsets))
        {
            writer.WriteFString(file.Path);
            WriteEntry(writer, offset, (ulong)file.Data.Length, file.Hash);
        }
        
        // Record data about index
        var indexSize = writer.BaseStream.Position - indexOffset;
        var indexHash = SHA1.HashData(new ReadOnlySpan<byte>(stream.GetBuffer(), (int)indexOffset, (int)indexSize));
        
        // Write footer
        for (var i = 0; i < 16; i++) // Encryption guid
        {
            writer.Write((byte)0);
        }
        writer.Write(encrypted);
        writer.Write(magic);
        writer.Write(versionMajor);
        writer.Write((ulong)indexOffset);
        writer.Write((ulong)indexSize);
        writer.Write(indexHash);
        for (var i = 0; i < algoSize; i++)
        {
            writer.Write(new byte[32]);
        }

        return stream.ToArray();
    }
}

internal static class BinaryWriterExt
{
    private static bool IsAscii(string str) => str.All(c => c <= 127);

    public static void WriteFString(this BinaryWriter writer, string str)
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