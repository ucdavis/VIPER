using System.Globalization;

namespace Viper.Areas.Personnel.Models.Eis;

/// <summary>
/// Turns campus directory (LDAP) attributes into <see cref="EisCampusListing"/>s. Kept apart from
/// the directory connection so the rules can be tested without a directory.
/// </summary>
public static class EisCampusListings
{
    /// <summary>The LDAP attributes the Address page shows.</summary>
    public static readonly IReadOnlyList<string> Attributes =
    [
        "title", "ou", "street", "l", "st", "postalCode", "telephoneNumber", "ucdPublish", "ucdListingOrder",
    ];

    /// <summary>ucdPublish value for a listing shown in the public directory.</summary>
    public const string PublicFlag = "W";

    public static EisCampusListing Create(IReadOnlyDictionary<string, string> attributes, bool isPrimary)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        bool isPublic = isPrimary
            || string.Equals(Value(attributes, "ucdPublish"), PublicFlag, StringComparison.OrdinalIgnoreCase);
        return new EisCampusListing(
            isPrimary,
            isPublic,
            Value(attributes, "title"),
            Value(attributes, "ou"),
            Address(attributes),
            Value(attributes, "telephoneNumber"));
    }

    /// <summary>
    /// The listing's position in the directory (ucdListingOrder), or <see cref="int.MaxValue"/>
    /// when it has none, so unnumbered listings sort last.
    /// </summary>
    public static int ListingOrder(IReadOnlyDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        return int.TryParse(Value(attributes, "ucdListingOrder"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int order)
            ? order
            : int.MaxValue;
    }

    /// <summary>"street, city, ST 95616", leaving out whichever parts are blank.</summary>
    private static string? Address(IReadOnlyDictionary<string, string> attributes)
    {
        string stateAndZip = JoinPresent(" ", Value(attributes, "st"), Value(attributes, "postalCode"));
        string address = JoinPresent(", ", Value(attributes, "street"), Value(attributes, "l"), stateAndZip);
        return address.Length > 0 ? address : null;
    }

    private static string JoinPresent(string separator, params string?[] parts)
    {
        return string.Join(separator, parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? Value(IReadOnlyDictionary<string, string> attributes, string name)
    {
        return attributes.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }
}
