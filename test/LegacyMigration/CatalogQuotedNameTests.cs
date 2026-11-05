using Viper.Scripts.LegacyMigration;

namespace Viper.test.LegacyMigration;

/// <summary>
/// The export names objects [schema].[name] so they can be pasted into SQL; a ] inside a name must be
/// doubled, or the name ends early.
/// </summary>
[InvariantCulture]
public class CatalogQuotedNameTests
{
    [Theory]
    [InlineData("dbo", "usp_Report", "[dbo].[usp_Report]")]
    [InlineData("dbo", "usp_Report]x", "[dbo].[usp_Report]]x]")]
    [InlineData("odd]schema", "v", "[odd]]schema].[v]")]
    public void QuotedName_DoublesAnyClosingBracket(string schema, string name, string expected)
    {
        Assert.Equal(expected, Catalog.QuotedName(schema, name));
    }
}
