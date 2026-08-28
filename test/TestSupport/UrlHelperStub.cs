using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Viper.test.TestSupport;

/// <summary>
/// ControllerBase.Url resolves IUrlHelperFactory from DI, which a bare DefaultHttpContext has no
/// registration for. Controller tests that reach the ReturnUrl guards set this stub instead.
/// </summary>
internal static class UrlHelperStub
{
    /// <summary>
    /// An IUrlHelper whose IsLocalUrl mirrors framework semantics: rooted "/..." and app-relative
    /// "~/..." are local, but protocol-relative ("//"), backslash ("/\\") and their "~/" variants
    /// are not.
    /// </summary>
    public static IUrlHelper Create()
    {
        var url = Substitute.For<IUrlHelper>();
        url.IsLocalUrl(Arg.Any<string?>()).Returns(ci => IsLocalUrl(ci.Arg<string?>()));
        return url;
    }

    private static bool IsLocalUrl(string? candidate)
    {
        if (string.IsNullOrEmpty(candidate))
        {
            return false;
        }

        if (candidate.StartsWith('/'))
        {
            return !candidate.StartsWith("//") && !candidate.StartsWith("/\\");
        }

        return candidate.StartsWith("~/")
            && !candidate.StartsWith("~//")
            && !candidate.StartsWith("~/\\");
    }
}
