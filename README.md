# 🎬 Mp4Parser

A modern, minimal MP4 / ISOBMFF box header parser for .NET — built for structural inspection, tooling, and automation.

## 🚀 Quickstart

```csharp
using Mp4Parser;

// PARSE FROM FILE
var boxes = Parser.Parse("video.mp4");

// OR FROM A STREAM
using var stream = File.OpenRead("video.mp4");
var boxes = Parser.Parse(stream);

// ASYNC VARIANT WITH CANCELLATION
var boxes = await Parser.ParseAsync("video.mp4", cancellationToken: cts.Token);

// PRINT THE BOX TREE
Parser.PrintTree(boxes, Console.Out);
```

Output:
```
[ftyp, size: 32, offset: 0]
[moov, size: 1024, offset: 32]
	[mvhd, size: 108, offset: 40]
	[trak, size: 900, offset: 148]
		[tkhd, size: 92, offset: 156]
		[mdia, size: 800, offset: 248]
			[mdhd, size: 32, offset: 256]
			[hdlr, size: 45, offset: 288]
			[minf, size: 715, offset: 333]
[mdat, size: 999936, offset: 1056]
```

## ✨ Features

- 📦 Full ISOBMFF box traversal (depth-first)
- 📐 32-bit and 64-bit box size support (largesize)
- 🪆 Nested container hierarchies (moov > trak > mdia > ...)
- 🔢 Proper big-endian binary reading via `BinaryPrimitives`
- 🔤 Latin-1 decoding for box types (handles `©nam` and friends)
- 📋 FullBox support for `meta` (version + flags skipped automatically)
- 🌊 Stream-based parsing — no full file buffering for seekable streams
- ⚡ Async overloads with `CancellationToken` support
- 🔒 Strict mode: throw `InvalidDataException` on malformed input
- 🎛️ Configurable container types and max depth via `Mp4ParseOptions`
- 🪶 Zero external dependencies

## 📦 Repository Structure

```
mp4-parser/
├── src/
│   └── Mp4Parser/            # CORE LIBRARY
│       ├── Parser.cs          # MAIN PARSER (SYNC + ASYNC)
│       ├── Mp4BoxHeader.cs    # BOX HEADER RECORD STRUCT
│       └── Mp4ParseOptions.cs # CONFIGURATION OPTIONS
├── tests/
│   └── Mp4Parser.Tests/      # XUNIT TESTS
│       └── ParserTests.cs
├── Directory.Build.props      # SHARED BUILD PROPERTIES
├── .editorconfig              # CODE STYLE RULES
├── mp4-parser.slnx            # SOLUTION FILE
├── build.cake                 # CAKE BUILD SCRIPT
└── LICENSE                    # MIT
```

## ⚙️ Technology Stack

- .NET 10 / C# 14
- Zero external dependencies (library)
- xUnit (tests)
- Cake (build automation)

## 🏗️ Build

```bash
# RESTORE + BUILD + TEST (VIA CAKE)
dotnet tool restore
dotnet cake

# OR MANUALLY
dotnet build mp4-parser.slnx
dotnet test mp4-parser.slnx
```

## 🧩 What This Is Not

Mp4Parser deliberately avoids:

- Decoding video or audio streams
- Media playback or transcoding
- Codec-level interpretation
- ffmpeg-style convenience APIs

If you need playback or transcoding, this is not the right tool.

## 🤝 Contributing

If you value clean parsing, predictable behavior, and spec-aligned tooling — contributions, ideas, and discussions are welcome.

## 📄 License

MIT — see [LICENSE](LICENSE).

---

*This project stays intentionally close to the metal. If you want to understand what is inside an MP4 file, you are exactly where you should be.*
