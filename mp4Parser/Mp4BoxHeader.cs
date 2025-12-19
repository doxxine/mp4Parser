namespace mp4Parser;

/// <summary>
/// REPRESENTS AN MP4/ISOBMFF BOX HEADER (TYPE, SIZE, OFFSET) PLUS PARSING CONTEXT.
/// </summary>
public readonly record struct Mp4BoxHeader(
    string Type,
    ulong Size,
    long Offset,
    int Level,
    int HeaderSize,
    long PayloadOffset,
    ulong PayloadSize)
{
    public override string ToString()
        => $"{new string('\t', Level)}[{Type}, size: {Size}, offset: {Offset}]";
}
