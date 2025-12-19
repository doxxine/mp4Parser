namespace mp4Parser;

/// <summary>
/// CONFIGURATION OPTIONS FOR THE MP4 BOX PARSER.
/// </summary>
public sealed class Mp4ParseOptions
{
    // NOTE: THIS CLASS USES C# 14'S 'field' KEYWORD FOR A FIELD-BACKED PROPERTY.

    /// <summary>
    /// MAXIMUM NESTING DEPTH TO PARSE.
    /// </summary>
    public int MaxDepth { get; init; } = 64;

    /// <summary>
    /// IF TRUE, THROW ON MALFORMED INPUT INSTEAD OF STOPPING EARLY.
    /// </summary>
    public bool Strict { get; init; } = false;

    /// <summary>
    /// SET OF BOX TYPES THAT SHOULD BE TREATED AS CONTAINERS (I.E., THEIR PAYLOAD IS PARSED FOR CHILD BOXES).
    /// </summary>
    public ISet<string> ContainerBoxTypes
    {
        get;
        init => field = value ?? throw new ArgumentNullException(nameof(value));
    } = new HashSet<string>(StringComparer.Ordinal)
    {
        // TOP-LEVEL AND COMMON CONTAINER BOXES.
        "moov",
        "trak",
        "mdia",
        "minf",
        "stbl",
        "edts",
        "dinf",
        "udta",
        "meta",
        "ilst",
        "moof",
        "traf",
        "mfra",
    };
}
