using System.Buffers.Binary;
using System.Text;
using Xunit;

namespace Mp4Parser.Tests;

public class ParserTests
{
    /// <summary>
    /// BUILDS A MINIMAL BOX: 4-BYTE SIZE (BIG-ENDIAN) + 4-BYTE TYPE (ASCII/LATIN-1) + OPTIONAL PAYLOAD.
    /// </summary>
    private static byte[] MakeBox(string type, byte[]? payload = null)
    {
        payload ??= [];
        int totalSize = 8 + payload.Length;
        var box = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(box, (uint)totalSize);
        Encoding.Latin1.GetBytes(type, box.AsSpan(4, 4));
        payload.CopyTo(box, 8);
        return box;
    }

    /// <summary>
    /// BUILDS A 64-BIT (LARGE SIZE) BOX: SIZE32=1, TYPE, 8-BYTE LARGE SIZE, PAYLOAD.
    /// </summary>
    private static byte[] MakeLargeSizeBox(string type, byte[]? payload = null)
    {
        payload ??= [];
        ulong totalSize = (ulong)(16 + payload.Length);
        var box = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(box, 1); // SIGNALS LARGE SIZE
        Encoding.Latin1.GetBytes(type, box.AsSpan(4, 4));
        BinaryPrimitives.WriteUInt64BigEndian(box.AsSpan(8), totalSize);
        payload.CopyTo(box, 16);
        return box;
    }

    /// <summary>
    /// BUILDS A ZERO-SIZE BOX (SIZE=0 MEANS "EXTENDS TO END OF CONTAINER").
    /// </summary>
    private static byte[] MakeZeroSizeBox(string type, byte[]? payload = null)
    {
        payload ??= [];
        var box = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(box, 0);
        Encoding.Latin1.GetBytes(type, box.AsSpan(4, 4));
        payload.CopyTo(box, 8);
        return box;
    }

    private static MemoryStream ToStream(params byte[][] boxes)
    {
        var ms = new MemoryStream();
        foreach (var box in boxes)
        {
            ms.Write(box);
        }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Parse_EmptyStream_ReturnsEmptyList()
    {
        using var stream = new MemoryStream([]);
        var result = Parser.Parse(stream);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_MinimalValidMp4_ParsesFtypAndMoov()
    {
        var ftyp = MakeBox("ftyp", [0x69, 0x73, 0x6F, 0x6D]); // "isom" BRAND
        var moov = MakeBox("moov", MakeBox("mvhd", new byte[100]));

        using var stream = ToStream(ftyp, moov);
        var result = Parser.Parse(stream);

        Assert.True(result.Count >= 2);
        Assert.Equal("ftyp", result[0].Type);
        Assert.Equal("moov", result[1].Type);
    }

    [Fact]
    public void Parse_NestedContainers_ParsesMoovTrakMdia()
    {
        var mdhd = MakeBox("mdhd", new byte[24]);
        var mdia = MakeBox("mdia", mdhd);
        var trak = MakeBox("trak", mdia);
        var moov = MakeBox("moov", trak);

        using var stream = ToStream(moov);
        var result = Parser.Parse(stream);

        Assert.Equal(4, result.Count);
        Assert.Equal("moov", result[0].Type);
        Assert.Equal(0, result[0].Level);
        Assert.Equal("trak", result[1].Type);
        Assert.Equal(1, result[1].Level);
        Assert.Equal("mdia", result[2].Type);
        Assert.Equal(2, result[2].Level);
        Assert.Equal("mdhd", result[3].Type);
        Assert.Equal(3, result[3].Level);
    }

    [Fact]
    public void Parse_LargeSizeBoxHeader_Parses64BitSize()
    {
        var payload = new byte[32];
        var largeBox = MakeLargeSizeBox("free", payload);

        using var stream = ToStream(largeBox);
        var result = Parser.Parse(stream);

        Assert.Single(result);
        Assert.Equal("free", result[0].Type);
        Assert.Equal(16, result[0].HeaderSize);
        Assert.Equal((ulong)(16 + payload.Length), result[0].Size);
    }

    [Fact]
    public void Parse_ZeroSizeBox_ExtendsToEnd()
    {
        var payload = new byte[20];
        var zeroBox = MakeZeroSizeBox("free", payload);

        using var stream = ToStream(zeroBox);
        var result = Parser.Parse(stream);

        Assert.Single(result);
        Assert.Equal("free", result[0].Type);
        Assert.Equal((ulong)(8 + payload.Length), result[0].Size);
    }

    [Fact]
    public void Parse_StrictMode_InvalidBoxSizeThrows()
    {
        // CRAFT A BOX WITH SIZE SMALLER THAN HEADER (SIZE=4, WHICH IS < 8 BYTE HEADER).
        var badBox = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(badBox, 4);
        Encoding.Latin1.GetBytes("bad!", badBox.AsSpan(4, 4));

        using var stream = new MemoryStream(badBox);
        var options = new Mp4ParseOptions { Strict = true };

        Assert.Throws<InvalidDataException>(() => Parser.Parse(stream, options));
    }

    [Fact]
    public void Parse_NonStrictMode_InvalidBoxSizeStopsGracefully()
    {
        var badBox = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(badBox, 4);
        Encoding.Latin1.GetBytes("bad!", badBox.AsSpan(4, 4));

        using var stream = new MemoryStream(badBox);
        var result = Parser.Parse(stream);

        Assert.Empty(result);
    }

    [Fact]
    public void Parse_NonSeekableStream_FallsBackToBuffering()
    {
        var ftyp = MakeBox("ftyp", [0x69, 0x73, 0x6F, 0x6D]);
        var data = new MemoryStream(ftyp);

        using var nonSeekable = new NonSeekableStream(data);
        var result = Parser.Parse(nonSeekable);

        Assert.Single(result);
        Assert.Equal("ftyp", result[0].Type);
    }

    [Fact]
    public void PrintTree_FormatsOutputCorrectly()
    {
        var boxes = new[]
        {
            new Mp4BoxHeader("moov", 100, 0, 0, 8, 8, 92),
            new Mp4BoxHeader("trak", 80, 8, 1, 8, 16, 72),
        };

        using var writer = new StringWriter();
        Parser.PrintTree(boxes, writer);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("[moov, size: 100, offset: 0]", lines[0]);
        Assert.Equal("\t[trak, size: 80, offset: 8]", lines[1]);
    }

    [Fact]
    public async Task ParseAsync_Stream_ReturnsIdenticalResults()
    {
        var mdhd = MakeBox("mdhd", new byte[24]);
        var mdia = MakeBox("mdia", mdhd);
        var trak = MakeBox("trak", mdia);
        var moov = MakeBox("moov", trak);

        var syncResult = Parser.Parse(new MemoryStream(moov));

        using var stream = new MemoryStream(moov);
        var asyncResult = await Parser.ParseAsync(stream);

        Assert.Equal(syncResult.Count, asyncResult.Count);
        for (int i = 0; i < syncResult.Count; i++)
        {
            Assert.Equal(syncResult[i], asyncResult[i]);
        }
    }

    [Fact]
    public async Task ParseAsync_NonSeekableStream_Works()
    {
        var ftyp = MakeBox("ftyp", [0x69, 0x73, 0x6F, 0x6D]);

        using var nonSeekable = new NonSeekableStream(new MemoryStream(ftyp));
        var result = await Parser.ParseAsync(nonSeekable);

        Assert.Single(result);
        Assert.Equal("ftyp", result[0].Type);
    }

    [Fact]
    public async Task ParseAsync_CancellationToken_ThrowsWhenCancelled()
    {
        var ftyp = MakeBox("ftyp", [0x69, 0x73, 0x6F, 0x6D]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var stream = new MemoryStream(ftyp);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Parser.ParseAsync(stream, cancellationToken: cts.Token));
    }

    /// <summary>
    /// A STREAM WRAPPER THAT DISABLES SEEKING TO TEST THE BUFFERING FALLBACK PATH.
    /// </summary>
    private sealed class NonSeekableStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => inner.ReadAsync(buffer, offset, count, ct);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => inner.ReadAsync(buffer, ct);
        public override void Flush() => inner.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void CopyTo(Stream destination, int bufferSize) => inner.CopyTo(destination, bufferSize);
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken ct) => inner.CopyToAsync(destination, bufferSize, ct);

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
