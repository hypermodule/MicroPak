# MicroPak

A minimal Unreal .pak file creator (making uncompressed "V8B" Unreal Pak files).

## Usage

**Standalone:**

Say you have a directory structure like this:

```
C/
└── MyMod_P/
    └── CoolGame/
        └── Content/
            ├── Materials/
            │   └── Instance/
            │       ├── MI_Example.uasset
            │       └── MI_Example.uexp
            └── Textures/
                ├── T_Example.uasset
                ├── T_Example.uexp
                └── T_Example.ubulk
```

Then you should be able to package `MyMod_P` like this:

```
C:\> \path\to\MicroPak.exe MyMod_P
```

**Library:**

There's only one method, `Pak.Create()`, which takes a `(path, bytes)` dictionary
and produces a .pak file. Example:

```csharp
Pak.Create(new Dictionary<string, byte[]>
{
    ["CoolGame/Content/Dir1/Dir2/FileA.txt"] = "Hello, world!"u8.ToArray(),
    ["CoolGame/Content/Dir1/Dir2/FileB.txt"] = "Goodbye, world!"u8.ToArray()
});
```

**Note:** The paths should be relative to the root directory (in the preceding
example that would be the `MyMod_P` directory).

## Credits and license

MicroPak is licensed under the MIT license. It was written by referencing the
code of [repak](https://github.com/trumank/repak), which was written by
[trumank](https://github.com/trumank) and [spuds](https://github.com/bananaturtlesandwich)
and likewise licensed under the MIT license.
