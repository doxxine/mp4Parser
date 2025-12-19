# 🎬 mp4Parser
A modern, minimal MP4 / ISOBMFF parser for structural inspection, tooling, and automation.

## Overview
**mp4Parser** is a lightweight, low-level parser for the **ISO Base Media File Format (ISOBMFF)**, commonly known as MP4.

It focuses exclusively on **container structure analysis** — boxes (atoms), hierarchies, sizes, offsets, and metadata blocks — without decoding audio or video streams.

The project originated around **2016** as internal tooling and has since been **fully modernized** to **.NET 10 / C# 14**, preserving the original parsing logic while aligning with current platform standards.

mp4Parser is designed as a precision tool for inspection, analysis, automation, and pipeline integration.

## ✨ Core Principles
### 1. Container-First Parsing
mp4Parser operates strictly at the container level.
No decoding, no interpretation of codecs — only what is defined by the ISOBMFF specification.

### 2. Stream-Based Design
All parsing is performed directly on streams.
Files are never fully loaded into memory, enabling safe handling of very large media files.

### 3. Minimalism Over Abstraction
The codebase avoids unnecessary layers, helpers, or dependencies.
The goal is clarity, predictability, and spec-aligned behavior.

### 4. Designed for Extension
The core parser provides a stable foundation that can be extended with:
- box-specific parsers
- metadata extraction layers
- validation and inspection tooling

## 🛠️ What mp4Parser Provides
mp4Parser focuses on structural analysis and automation-friendly workflows:

✔ Full ISOBMFF box traversal  
✔ 32‑bit and 64‑bit box size support (largesize)  
✔ Nested box hierarchies  
✔ Proper big-endian binary reading  
✔ Latin‑1 decoding for box types (e.g. ©nam)  
✔ FullBox support (version + flags)  
✔ Stream-based parsing (no full file buffering)  

## 🧩 What mp4Parser Is Not
mp4Parser deliberately avoids:

- decoding video or audio
- media playback
- transcoding or remuxing
- codec-level interpretation
- ffmpeg-style convenience APIs

If you need playback or transcoding, this is not the right tool.

## 📦 Project Structure
```
mp4Parser.sln
│
├─ mp4Parser/            # CORE LIBRARY (.NET 10)
│   ├─ Mp4Parser.cs
│   └─ mp4Parser.csproj
│
└─ mp4Parser.Cli/        # SMALL CLI TOOL
    ├─ Program.cs
    └─ mp4Parser.Cli.csproj
```

## ⚙️ Technology Stack
- .NET 10
- C# 14
- SDK-style projects
- Nullable reference types enabled
- Implicit usings enabled
- Zero external dependencies

No legacy `App.config`  
No `AssemblyInfo.cs`  
No classic MSBuild artifacts  

## ▶️ CLI Usage
### Build
```
dotnet build mp4Parser.sln -c Release
```

### Parse a local file
```
dotnet run --project mp4Parser.Cli -- ./video.mp4
```

### Parse with JSON output
```
dotnet run --project mp4Parser.Cli -- ./video.mp4 --json
```

### Parse a remote file (HTTP/S)
```
dotnet run --project mp4Parser.Cli -- https://example.com/video.mp4
```
(The CLI downloads the file to a temporary location before parsing.)

## 📚 Library Usage
```csharp
using Mp4Parser;

using var stream = File.OpenRead("video.mp4");

var parser = new Mp4Parser();
var boxes = parser.Parse(stream);

foreach (var box in boxes)
{
    Console.WriteLine($"{box.Type} @ {box.Offset} ({box.Size} bytes)");
}
```

## 🧪 Legacy API
Some original APIs are still present for compatibility:

- `parserFunction`
- `getTypes`
- `PrintHeader`

They are marked as obsolete:
```csharp
[Obsolete("LEGACY API – USE Parse() INSTEAD")]
```

These APIs will be removed once no longer required.

## 🚦 Status
mp4Parser is stable and under light active development.

Planned extensions include:
- box-specific parsers (moov, trak, mdia, stbl, …)
- structured metadata extraction
- async stream support
- fMP4 / CMAF inspection helpers
- optional JSON or graph-based visualization output

## 📄 License
mp4Parser is released under the **MIT License**.

## 🙌 Contributing
If you value clean parsing, predictable behavior, and spec-aligned tooling,
contributions, ideas, and discussions are welcome.

---

This project stays intentionally close to the metal.

If you want to understand what is inside an MP4 file,
you are exactly where you should be.
