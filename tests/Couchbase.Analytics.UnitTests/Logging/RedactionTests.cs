using Couchbase.AnalyticsClient.Logging;
using Xunit;

namespace Couchbase.AnalyticsClient.UnitTests.Logging;

public class RedactionTests
{
    // ─── RedactionLevel.None ───

    [Fact]
    public void None_UserData_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        Assert.Equal("user-value", redactor.UserData("user-value").ToString());
    }

    [Fact]
    public void None_MetaData_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        Assert.Equal("meta-value", redactor.MetaData("meta-value").ToString());
    }

    [Fact]
    public void None_SystemData_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        Assert.Equal("system-value", redactor.SystemData("system-value").ToString());
    }

    // ─── RedactionLevel.Partial — only UserData gets tagged ───

    [Fact]
    public void Partial_UserData_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Partial);
        Assert.Equal("<ud>user-value</ud>", redactor.UserData("user-value").ToString());
    }

    [Fact]
    public void Partial_MetaData_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Partial);
        Assert.Equal("meta-value", redactor.MetaData("meta-value").ToString());
    }

    [Fact]
    public void Partial_SystemData_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Partial);
        Assert.Equal("system-value", redactor.SystemData("system-value").ToString());
    }

    // ─── RedactionLevel.Full — everything gets tagged ───

    [Fact]
    public void Full_UserData_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        Assert.Equal("<ud>user-value</ud>", redactor.UserData("user-value").ToString());
    }

    [Fact]
    public void Full_MetaData_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        Assert.Equal("<md>meta-value</md>", redactor.MetaData("meta-value").ToString());
    }

    [Fact]
    public void Full_SystemData_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        Assert.Equal("<sd>system-value</sd>", redactor.SystemData("system-value").ToString());
    }

    // ─── Typed values (Uri, int) — verifies generic <T> works ───

    [Fact]
    public void Full_SystemData_Uri_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        var uri = new Uri("http://localhost:8095/api/v1/request");
        Assert.Equal("<sd>http://localhost:8095/api/v1/request</sd>", redactor.SystemData(uri).ToString());
    }

    [Fact]
    public void None_SystemData_Uri_NotRedacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        var uri = new Uri("http://localhost:8095/api/v1/request");
        Assert.Equal("http://localhost:8095/api/v1/request", redactor.SystemData(uri).ToString());
    }

    [Fact]
    public void Full_UserData_Int_Redacted()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        Assert.Equal("<ud>42</ud>", redactor.UserData(42).ToString());
    }

    // ─── Null handling ───

    [Fact]
    public void None_NullValue_ReturnsEmptyString()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        Assert.Equal("", redactor.UserData((string?)null).ToString());
    }

    [Fact]
    public void Full_NullValue_ReturnsRedactedEmpty()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        Assert.Equal("<ud></ud>", redactor.UserData((string?)null).ToString());
    }

    // ─── ISpanFormattable (string interpolation path) ───

    [Fact]
    public void Full_SpanFormattable_WorksInInterpolation()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        var result = $"host={redactor.SystemData("192.168.1.1")} port=8095";
        Assert.Equal("host=<sd>192.168.1.1</sd> port=8095", result);
    }

    [Fact]
    public void None_SpanFormattable_WorksInInterpolation()
    {
        var redactor = new TypedRedactor(RedactionLevel.None);
        var result = $"host={redactor.SystemData("192.168.1.1")} port=8095";
        Assert.Equal("host=192.168.1.1 port=8095", result);
    }

    // ─── TryFormat directly ───

    [Fact]
    public void Full_TryFormat_WritesToSpan()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        var redacted = redactor.SystemData("test");

        Span<char> buffer = stackalloc char[64];
        Assert.True(redacted.TryFormat(buffer, out var written, default, null));
        Assert.Equal("<sd>test</sd>", buffer[..written].ToString());
    }

    [Fact]
    public void TryFormat_BufferTooSmall_ReturnsFalse()
    {
        var redactor = new TypedRedactor(RedactionLevel.Full);
        var redacted = redactor.SystemData("a-value-that-needs-tags");

        Span<char> buffer = stackalloc char[2]; // way too small
        Assert.False(redacted.TryFormat(buffer, out _, default, null));
    }

    // ─── RedactionLevel property ───

    [Theory]
    [InlineData(RedactionLevel.None)]
    [InlineData(RedactionLevel.Partial)]
    [InlineData(RedactionLevel.Full)]
    public void RedactionLevel_IsExposed(RedactionLevel level)
    {
        var redactor = new TypedRedactor(level);
        Assert.Equal(level, redactor.RedactionLevel);
    }
}
