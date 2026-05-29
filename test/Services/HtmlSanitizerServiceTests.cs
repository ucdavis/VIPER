using Viper.Services;

namespace Viper.test.Services;

/// <summary>
/// Locks the sanitizer's policy in place. Failures here indicate the allowlist or
/// XSS handling has shifted — investigate before changing assertions.
/// </summary>
public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _sanitizer = new();

    // === XSS regressions ===

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<SCRIPT>alert(1)</SCRIPT>")]
    [InlineData("<scr<script>ipt>alert(1)</script>")]
    public void Strips_script_tags(string input)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("<script", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(1)", output);
    }

    [Theory]
    [InlineData("onerror", "<img src=\"x\" onerror=\"alert(1)\" alt=\"x\">")]
    [InlineData("onclick", "<a href=\"#\" onclick=\"alert(1)\">x</a>")]
    [InlineData("onload", "<div onload=\"alert(1)\">x</div>")]
    [InlineData("onmouseover", "<span onmouseover=\"alert(1)\">x</span>")]
    public void Strips_event_handler_attributes(string handler, string input)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain(handler, output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(1)", output);
    }

    [Theory]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<a href=\"JAVASCRIPT:alert(1)\">x</a>")]
    [InlineData("<a href=\"vbscript:alert(1)\">x</a>")]
    public void Strips_dangerous_url_schemes_in_href(string input)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("javascript:", output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vbscript:", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strips_data_uri_in_img_src()
    {
        var input = "<img src=\"data:image/svg+xml;base64,PHN2Zw==\" alt=\"x\">";
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("data:", output);
    }

    [Fact]
    public void Strips_javascript_url_inside_css()
    {
        var input = "<div style=\"background: url(javascript:alert(1))\">x</div>";
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("javascript:", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Strips_expression_in_css()
    {
        var input = "<div style=\"width: expression(alert(1))\">x</div>";
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("expression", output, StringComparison.OrdinalIgnoreCase);
    }

    // === Absolute <img src> is rejected — relative-only policy (legacy onsiteURL regex) ===
    // Includes same-origin absolutes: the policy is strictly "no scheme, no host", not "no cross-origin".

    [Theory]
    [InlineData("<img src=\"https://example.com/tracker.png\" alt=\"x\">", "example.com")]
    [InlineData("<img src=\"http://example.com/tracker.png\" alt=\"x\">", "example.com")]
    [InlineData("<img src=\"//example.com/tracker.png\" alt=\"x\">", "example.com")]
    [InlineData("<img src=\"https://secure-test.vetmed.ucdavis.edu/CMS/Files?fn=photo.jpg\" alt=\"x\">", "vetmed.ucdavis.edu")]
    public void Strips_offsite_img_src(string input, string hostFragment)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain(hostFragment, output, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<img src=\"/photo.jpg\" alt=\"x\">", "src=\"/photo.jpg\"")]
    [InlineData("<img src=\"/CMS/Files?fn=report.pdf\" alt=\"x\">", "src=\"/CMS/Files?fn=report.pdf\"")]
    [InlineData("<img src=\"images/photo.jpg\" alt=\"x\">", "src=\"images/photo.jpg\"")]
    public void Preserves_onsite_img_src(string input, string mustContain)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.Contains(mustContain, output);
    }

    [Fact]
    public void Preserves_offsite_anchor_href()
    {
        var input = "<a href=\"https://example.com\">x</a>";
        var output = _sanitizer.Sanitize(input);
        Assert.Contains("href=\"https://example.com\"", output);
    }

    // === Allowlist parity vs legacy AntiSamy (deliberately blocked) ===

    [Theory]
    [InlineData("<form")]
    [InlineData("<input")]
    [InlineData("<textarea")]
    [InlineData("<select")]
    [InlineData("<option")]
    [InlineData("<button")]
    [InlineData("<label")]
    public void Strips_form_related_tags(string tagOpener)
    {
        var input = $"<form action=\"evil\"><input name=\"u\"><textarea>x</textarea>" +
                    $"<select><option>x</option></select><button>x</button><label>x</label></form>";
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain(tagOpener, output, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<tt>monospace</tt>")]
    [InlineData("<ecode>legacy</ecode>")]
    [InlineData("<quote>legacy</quote>")]
    public void Strips_legacy_custom_tags(string input)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain("<tt", output);
        Assert.DoesNotContain("<ecode", output);
        Assert.DoesNotContain("<quote", output);
    }

    // === Allowed constructs preserved ===

    [Theory]
    [InlineData("<figure><figcaption>caption</figcaption></figure>", "<figure>", "<figcaption>")]
    [InlineData("<dl><dt>term</dt><dd>def</dd></dl>", "<dl>", "<dt>")]
    [InlineData("<a target=\"_blank\" rel=\"noopener\" href=\"https://example.com\">x</a>", "target=\"_blank\"", "rel=\"noopener\"")]
    [InlineData("<a href=\"https://example.com/file.pdf\" download=\"file.pdf\">x</a>", "download=\"file.pdf\"", "href=\"https://example.com/file.pdf\"")]
    [InlineData("<table><thead><tr><th scope=\"col\">h</th></tr></thead></table>", "<thead>", "scope=\"col\"")]
    public void Preserves_allowed_constructs(string input, string mustContain1, string mustContain2)
    {
        var output = _sanitizer.Sanitize(input);
        Assert.Contains(mustContain1, output);
        Assert.Contains(mustContain2, output);
    }

    // === CSS allowlist ===

    [Theory]
    [InlineData("text-align", "center")]
    [InlineData("width", "100px")]
    [InlineData("height", "100px")]
    [InlineData("float", "left")]
    [InlineData("color", "red")]
    [InlineData("font-family", "Arial")]
    [InlineData("font-size", "12px")]
    [InlineData("font-weight", "bold")]
    [InlineData("margin-top", "10px")]
    [InlineData("padding-left", "10px")]
    [InlineData("vertical-align", "top")]
    [InlineData("text-decoration", "underline")]
    public void Preserves_allowed_css_property(string propName, string value)
    {
        var input = $"<div style=\"{propName}: {value}\">x</div>";
        var output = _sanitizer.Sanitize(input);
        Assert.True(output.Contains(propName, StringComparison.OrdinalIgnoreCase),
            $"Expected '{propName}' in output. Input=[{input}] Output=[{output}]");
    }

    [Theory]
    [InlineData("position")]
    [InlineData("z-index")]
    [InlineData("opacity")]
    [InlineData("transform")]
    public void Strips_disallowed_css_property(string propName)
    {
        var input = $"<div style=\"{propName}: 1\">x</div>";
        var output = _sanitizer.Sanitize(input);
        Assert.DoesNotContain(propName, output, StringComparison.OrdinalIgnoreCase);
    }

    // === Idempotency — guards against the double/triple-sanitization bug class ===

    [Theory]
    [InlineData("<p>simple</p>")]
    [InlineData("<div style=\"text-align: center;\"><a href=\"https://example.com\">link</a></div>")]
    [InlineData("<table><tr><td style=\"width: 220px;\">x</td></tr></table>")]
    [InlineData("<script>alert(1)</script><p>real content</p>")]
    public void Sanitization_is_idempotent(string input)
    {
        var first = _sanitizer.Sanitize(input);
        var second = _sanitizer.Sanitize(first);
        Assert.Equal(first, second);
    }

    // === Real-world snapshots from production ContentBlock data ===
    // If these break, several admin pages will visibly regress — review before relaxing.

    [Theory]
    [InlineData(
        "<div style=\"float: left; margin-right: 20px;\"><img src=\"/photo.jpg\" alt=\"x\" style=\"width: 87px; height: 111px;\"></div>",
        "float", "margin-right", "width", "height")]
    [InlineData(
        "<p style=\"text-align: center;\">centered</p>",
        "text-align", "center")]
    [InlineData(
        "<table><tr><td style=\"width: 50%; float: left;\">left</td><td style=\"width: 45%;\">right</td></tr></table>",
        "width", "float")]
    [InlineData(
        "<a href=\"/CMS/Files?fn=report.pdf\" target=\"_blank\">Report</a>",
        "href=\"/CMS/Files?fn=report.pdf\"", "target=\"_blank\"")]
    public void Preserves_realworld_inline_styles(string input, params string[] mustContain)
    {
        var output = _sanitizer.Sanitize(input);
        foreach (var token in mustContain)
        {
            Assert.Contains(token, output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
