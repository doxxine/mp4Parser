using System.Buffers.Binary;
using System.Text;

namespace mp4Parser;

/// <summary>
/// SIMPLE MP4/ISOBMFF BOX HEADER PARSER.
/// </summary>
public sealed class Parser
{
    /// <summary>
    /// PARSES A LOCAL MP4/ISOBMFF FILE AND RETURNS A DEPTH-FIRST LIST OF BOX HEADERS.
    /// </summary>
    public static IReadOnlyList<Mp4BoxHeader> Parse(string filePath, Mp4ParseOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = File.OpenRead(filePath);
        return Parse(stream, options);
    }

    /// <summary>
    /// PARSES AN MP4/ISOBMFF STREAM AND RETURNS A DEPTH-FIRST LIST OF BOX HEADERS.
    /// </summary>
    /// <remarks>
    /// THIS METHOD REQUIRES A SEEKABLE STREAM FOR BEST RESULTS. IF THE STREAM IS NOT SEEKABLE,
    /// IT WILL BE BUFFERED INTO MEMORY.
    /// </remarks>
    public static IReadOnlyList<Mp4BoxHeader> Parse(Stream stream, Mp4ParseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= new Mp4ParseOptions();

        if (!stream.CanSeek)
        {
            // TODO: IMPLEMENT A TRUE STREAMING PARSER THAT DOES NOT REQUIRE SEEK.
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;

            var bufferedOutput = new List<Mp4BoxHeader>(capacity: 256);
            ParseInternal(buffer, start: 0, length: buffer.Length, level: 0, options, bufferedOutput);
            return bufferedOutput;
        }

        // ALWAYS PARSE FROM THE BEGINNING OF THE STREAM FOR CONSISTENCY.
        stream.Position = 0;

        var output = new List<Mp4BoxHeader>(capacity: 256);
        ParseInternal(stream, start: 0, length: stream.Length, level: 0, options, output);
        return output;
    }

    /// <summary>
    /// PRINTS THE BOX TREE TO A TEXT WRITER USING TAB INDENTATION.
    /// </summary>
    public static void PrintTree(IEnumerable<Mp4BoxHeader> boxes, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(boxes);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var box in boxes)
        {
            writer.WriteLine(box);
        }
    }

    private static void ParseInternal(
        Stream stream,
        long start,
        long length,
        int level,
        Mp4ParseOptions options,
        List<Mp4BoxHeader> output)
    {
        long end = checked(start + length);
        long offset = start;

        while (offset + 8 <= end)
        {
            stream.Position = offset;

            Span<byte> header = stackalloc byte[8];
            int read = stream.Read(header);
            if (read < header.Length)
            {
                return;
            }

            uint size32 = BinaryPrimitives.ReadUInt32BigEndian(header[..4]);
            string type = Encoding.Latin1.GetString(header[4..8]);

            int headerSize = 8;
            ulong size = size32 switch
            {
                0 => (ulong)(end - offset), // BOX EXTENDS TO END OF ITS CONTAINER.
                1 => ReadLargeSize(stream, type, offset, options, out headerSize),
                _ => size32,
            };

            if (size < (ulong)headerSize)
            {
                FailOrReturn(options, $"INVALID BOX SIZE {size} FOR '{type}' AT OFFSET {offset}.");
                return;
            }

            if (size > long.MaxValue)
            {
                FailOrReturn(options, $"BOX '{type}' AT OFFSET {offset} IS TOO LARGE FOR THIS PARSER. SIZE={size}.");
                return;
            }

            long boxEnd = offset + (long)size;
            if (boxEnd > end)
            {
                FailOrReturn(options, $"BOX '{type}' AT OFFSET {offset} OVERFLOWS ITS CONTAINER. SIZE={size}, CONTAINER_END={end}.");
                return;
            }

            long payloadOffset = offset + headerSize;
            ulong payloadSize = size - (ulong)headerSize;

            output.Add(new Mp4BoxHeader(
                Type: type,
                Size: size,
                Offset: offset,
                Level: level,
                HeaderSize: headerSize,
                PayloadOffset: payloadOffset,
                PayloadSize: payloadSize));

            if (ShouldParseChildren(type, payloadSize, level, options))
            {
                long childStart = payloadOffset;
                long childLength = (long)payloadSize;

                if (type == "meta")
                {
                    // META IS A FULL BOX; ITS PAYLOAD STARTS WITH 4 BYTES (VERSION + FLAGS).
                    const int metaFullBoxHeaderBytes = 4;
                    if (childLength >= metaFullBoxHeaderBytes)
                    {
                        childStart += metaFullBoxHeaderBytes;
                        childLength -= metaFullBoxHeaderBytes;
                    }
                    else
                    {
                        FailOrReturn(options, $"META BOX TOO SMALL FOR FULL-BOX HEADER AT OFFSET {offset}.");
                        return;
                    }
                }

                ParseInternal(stream, childStart, childLength, level + 1, options, output);
            }

            offset = boxEnd;
        }
    }

    private static bool ShouldParseChildren(string type, ulong payloadSize, int level, Mp4ParseOptions options)
    {
        if (payloadSize < 8)
        {
            return false;
        }

        if (level >= options.MaxDepth)
        {
            return false;
        }

        // MDAT CAN BE HUGE; SKIP ITS PAYLOAD BY DEFAULT.
        if (type == "mdat")
        {
            return false;
        }

        return options.ContainerBoxTypes.Contains(type);
    }

    private static ulong ReadLargeSize(Stream stream, string type, long offset, Mp4ParseOptions options, out int headerSize)
    {
        Span<byte> largeSizeBytes = stackalloc byte[8];
        int read = stream.Read(largeSizeBytes);
        if (read < largeSizeBytes.Length)
        {
            FailOrReturn(options, $"TRUNCATED LARGE SIZE FOR BOX '{type}' AT OFFSET {offset}.");
            headerSize = 16;
            return 0;
        }

        headerSize = 16;
        return BinaryPrimitives.ReadUInt64BigEndian(largeSizeBytes);
    }

    private static void FailOrReturn(Mp4ParseOptions options, string message)
    {
        if (options.Strict)
        {
            throw new InvalidDataException(message);
        }
    }

    // --------------------
    // LEGACY API (KEPT FOR COMPATIBILITY WITH THE 2016 VERSION)
    // --------------------

    [Obsolete("LEGACY API: USE Parse(...) AND PrintTree(...). THIS METHOD WILL BE REMOVED IN A FUTURE MAJOR VERSION.")]
    public void parserFunction(string path)
    {
        var boxes = Parse(path);
        PrintTree(boxes, Console.Out);
    }

    [Obsolete("LEGACY API: USE Mp4BoxHeader.ToString() INSTEAD.")]
    public static string PrintHeader(string atomType, uint size, uint offset, int lvl)
        => $"{new string('\t', lvl)}[{atomType}, size: {size}, offset: {offset}]";

    [Obsolete("LEGACY API: USE Mp4ParseOptions.ContainerBoxTypes INSTEAD.")]
    public static string[] getTypes()
        =>
        [
            "ftyp,moov,mdat",
            "mvhd,trak,udta",
            "tkhd,edts,mdia,meta,covr,©nam",
            "mdhd,hdlr,minf",
            "smhd,vmhd,dinf,stbl",
            "stsd,stts,stss,ctts,stsc,stsz,stco",
        ];
}
