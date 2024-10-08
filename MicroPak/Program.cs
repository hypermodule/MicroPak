﻿using MicroPak;

if (args.Length != 1)
{
    Console.Error.WriteLine("Error: Incorrect number of arguments");
    Console.Error.WriteLine("Usage: MicroPak DIRECTORY");
    Environment.Exit(1);
}

if (!Directory.Exists(args[0]))
{
    Console.Error.WriteLine($"Error: '{args[1]}' is not a directory");
    Console.Error.WriteLine("Usage: MicroPak DIRECTORY");
    Environment.Exit(1);
}

var rootPath = args[0];

var dirName = Path.GetFileName(Path.TrimEndingDirectorySeparator(rootPath));

var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories).Select(path =>
{
    var relPath = Path.GetRelativePath(rootPath, path).Replace('\\', '/');
    var content = File.ReadAllBytes(path);

    return new KeyValuePair<string, byte[]>(relPath, content);
}).ToDictionary();

var pak = Pak.Create(files);

var pakName = dirName + ".pak";

File.WriteAllBytes(pakName, pak);

Console.WriteLine($"Packed {files.Count} files to {pakName}");