using System.Text;
using Viper.Areas.CMS.Data;

namespace Viper.test.CMS;

// Guards the UU encoder/decoder in Viper.Areas.CMS.Data.Codecs, which replicates
// ColdFusion's default "UU" encrypt()/decrypt() encoding. Round-trips prove encode
// and decode stay mutually consistent; the canonical-vector tests pin the uuencode
// wire format itself (the format legacy CF emits).
// UUEncode is retained for CMS-migration key-storage interop: new files must store
// their AES data key UU-encoded so the legacy CF system can still decrypt them during
// the parallel-run period (see PLAN-CMS.md s12.6), so it is exercised here too.
// Slated to move with Codecs into a dedicated CMS project.
public class CodecsTests
{
    private static byte[] RunCodec(Action<Stream, Stream> codec, byte[] input)
    {
        using var inStream = new MemoryStream(input, writable: false);
        using var outStream = new MemoryStream();
        codec(inStream, outStream);
        return outStream.ToArray();
    }

    // Deterministic, non-trivial byte pattern so failures are reproducible.
    private static byte[] MakeData(int length)
    {
        var data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = (byte)((i * 31 + 7) & 0xFF);
        return data;
    }

    #region Round-trip Tests

    // Lengths bracket the 45-byte line boundary and every encode tail branch
    // (full 3-byte groups, 2-byte remainder, 1-byte remainder, empty).
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(44)]
    [InlineData(45)]
    [InlineData(46)]
    [InlineData(47)]
    [InlineData(48)]
    [InlineData(89)]
    [InlineData(90)]
    [InlineData(91)]
    [InlineData(135)]
    [InlineData(1000)]
    public void UUEncode_ThenUUDecode_ReturnsOriginal(int length)
    {
        var data = MakeData(length);
        var roundTripped = RunCodec(Codecs.UUDecode, RunCodec(Codecs.UUEncode, data));
        Assert.Equal(data, roundTripped);
    }

    [Fact]
    public void UUEncode_ThenUUDecode_IsBinarySafeAcrossAllByteValues()
    {
        var data = new byte[256];
        for (int i = 0; i < 256; i++)
            data[i] = (byte)i;

        var roundTripped = RunCodec(Codecs.UUDecode, RunCodec(Codecs.UUEncode, data));
        Assert.Equal(data, roundTripped);
    }

    #endregion

    #region Wire-format Tests

    // "Cat" is the canonical uuencode example: a single data line whose first
    // byte is the length char '#' (3) followed by "0V%T". Pins the format
    // independently of the decoder, so a compensating encode+decode change
    // can't hide behind the round-trip tests.
    [Fact]
    public void UUEncode_KnownInput_ProducesCanonicalUuencodedLine()
    {
        var encoded = Encoding.ASCII.GetString(RunCodec(Codecs.UUEncode, Encoding.ASCII.GetBytes("Cat")));
        Assert.Equal("#0V%T", encoded.TrimEnd('\r', '\n'));
    }

    [Fact]
    public void UUDecode_CanonicalUuencodedLine_ProducesOriginalBytes()
    {
        var decoded = RunCodec(Codecs.UUDecode, Encoding.ASCII.GetBytes("#0V%T\r\n"));
        Assert.Equal("Cat", Encoding.ASCII.GetString(decoded));
    }

    #endregion

    #region Empty-input Tests

    [Fact]
    public void UUEncode_EmptyInput_WritesNothing() => Assert.Empty(RunCodec(Codecs.UUEncode, []));

    [Fact]
    public void UUDecode_EmptyInput_WritesNothing() => Assert.Empty(RunCodec(Codecs.UUDecode, []));

    #endregion

    #region Null-argument Tests

    [Fact]
    public void UUEncode_NullInput_ThrowsArgumentNullException()
    {
        using var output = new MemoryStream();
        var ex = Assert.Throws<ArgumentNullException>(() => Codecs.UUEncode(null!, output));
        Assert.Equal("input", ex.ParamName);
    }

    [Fact]
    public void UUEncode_NullOutput_ThrowsArgumentNullException()
    {
        using var input = new MemoryStream([1, 2, 3]);
        var ex = Assert.Throws<ArgumentNullException>(() => Codecs.UUEncode(input, null!));
        Assert.Equal("output", ex.ParamName);
    }

    [Fact]
    public void UUDecode_NullInput_ThrowsArgumentNullException()
    {
        using var output = new MemoryStream();
        var ex = Assert.Throws<ArgumentNullException>(() => Codecs.UUDecode(null!, output));
        Assert.Equal("input", ex.ParamName);
    }

    [Fact]
    public void UUDecode_NullOutput_ThrowsArgumentNullException()
    {
        using var input = new MemoryStream([1, 2, 3]);
        var ex = Assert.Throws<ArgumentNullException>(() => Codecs.UUDecode(input, null!));
        Assert.Equal("output", ex.ParamName);
    }

    [Fact]
    public void UUDecode_OutOfRangeByte_ThrowsFormatException()
    {
        // A non-ASCII byte (>= 128) cannot index the 128-entry decode table; malformed
        // input must surface as a controlled FormatException, not IndexOutOfRangeException.
        using var input = new MemoryStream([0xFF]);
        using var output = new MemoryStream();
        Assert.Throws<FormatException>(() => Codecs.UUDecode(input, output));
    }

    [Fact]
    public void UUDecode_TruncatedLine_ThrowsFormatException()
    {
        // Line header claims three octets but the encoded quartet is cut short (EOF mid-read).
        using var input = new MemoryStream(Encoding.ASCII.GetBytes("#0V"));
        using var output = new MemoryStream();
        Assert.Throws<FormatException>(() => Codecs.UUDecode(input, output));
    }

    #endregion
}
