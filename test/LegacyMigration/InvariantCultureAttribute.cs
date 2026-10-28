using System.Globalization;
using System.Reflection;
using Xunit.v3;

namespace Viper.test.LegacyMigration;

/// <summary>
/// Runs each test under the invariant culture, as the toolkit's Program.Main sets it, so assertions on
/// formatted counts (1,234 and 10.6%) hold whatever the machine's settings. Restores the culture after.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class InvariantCultureAttribute : BeforeAfterTestAttribute
{
    private CultureInfo? _culture;
    private CultureInfo? _uiCulture;

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _culture = CultureInfo.CurrentCulture;
        _uiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _culture ?? CultureInfo.CurrentCulture;
        CultureInfo.CurrentUICulture = _uiCulture ?? CultureInfo.CurrentUICulture;
    }
}
