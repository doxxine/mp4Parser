# mp4Parser

> MODERN, MINIMAL, NO-BULLSHIT MP4 / ISOBMFF PARSER  
> ORIGINAL CODEBASE ~2016 · FULLY MODERNIZED TO .NET 10 / C# 14

---

## WHAT THIS IS

`mp4Parser` is a **low-level MP4 / ISO Base Media File Format (ISOBMFF) parser**.

It was originally written around **2016** (C# 5/6 era) for internal tooling and media analysis,
and has now been **fully modernized** to current .NET standards while **preserving the original parsing logic and intent**.

This is **NOT** a media player  
This is **NOT** ffmpeg  
This **DOES NOT** decode video or audio  

It **parses container structure**:
- Boxes / atoms
- Hierarchies
- Sizes (incl. `largesize`)
- Metadata blocks
- Binary layout inspection

Think **forensics**, **tooling**, **pipelines**, **inspection**, **automation**.

---

## PROJECT STRUCTURE

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

---

## TECHNOLOGY STACK

- **.NET 10**
- **C# 14**
- SDK-style projects
- Nullable reference types enabled
- Implicit usings enabled
- Zero external dependencies

NO LEGACY `App.config`  
NO `AssemblyInfo.cs`  
NO OLD MSBUILD MAGIC  

---

## FEATURES

- FULL ISOBMFF BOX WALKER
- SUPPORTS:
  - 32-bit and 64-bit box sizes
  - Nested box hierarchies
  - `meta` as FULL BOX
  - Proper BIG-ENDIAN reading
  - Latin-1 decoding for box types (e.g. `©nam`)
- STREAM-BASED (NO FULL FILE LOAD)
- DESIGNED FOR EXTENSION

---

## CLI USAGE

### BUILD

```bash
dotnet build mp4Parser.sln -c Release
```

### PARSE LOCAL FILE

```bash
dotnet run --project mp4Parser.Cli -- ./video.mp4
```

### PARSE WITH JSON OUTPUT

```bash
dotnet run --project mp4Parser.Cli -- ./video.mp4 --json
```

### PARSE REMOTE FILE (HTTP/S)

```bash
dotnet run --project mp4Parser.Cli -- https://example.com/video.mp4
```

(The CLI downloads to a temp file and parses it.)

---

## LIBRARY USAGE

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

---

## LEGACY API

Some original APIs are still present for compatibility:

- `parserFunction`
- `getTypes`
- `PrintHeader`

They are marked:

```csharp
[Obsolete("LEGACY API – USE Parse() INSTEAD")]
```

They will be removed once no longer needed.

---

## DESIGN PHILOSOPHY

- **CONTAINER FIRST**
- **NO MAGIC**
- **NO GUESSING**
- **STRUCTURE OVER CONVENIENCE**
- **EXTENSIBLE BY DESIGN**

---

## TODO / ROADMAP

- [ ] BOX-SPECIFIC PARSERS (moov, trak, mdia, stbl, …)
- [ ] FULL METADATA EXTRACTION
- [ ] ASYNC STREAM SUPPORT
- [ ] STREAMING PIPELINE INTEGRATION
- [ ] FMP4 / CMAF VALIDATION HELPERS
- [ ] OPTIONAL VISUALIZATION EXPORT (JSON / GRAPH)

---

## LICENSE

MIT

USE IT. FORK IT. BREAK IT. FIX IT.

---

## FINAL NOTE

This project intentionally stays **close to the metal**.

If you are looking for:
- Playback → wrong tool
- Transcoding → wrong tool
- Convenience → wrong tool

If you want to **understand what’s inside an MP4**  
you’re exactly where you should be.
