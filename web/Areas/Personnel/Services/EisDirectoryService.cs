using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.Versioning;
using Viper.Areas.Personnel.Models.Eis;
using Viper.Classes.Utilities;

namespace Viper.Areas.Personnel.Services;

/// <summary>
/// Reads an employee's campus directory listings for the EIS Address page.
/// </summary>
public interface IEisDirectoryService
{
    /// <summary>
    /// The primary listing (found by employee number) followed by any additional listings for the
    /// same person (found by MothraID), or null when the directory can't be reached. Returns an
    /// empty list when the employee has no primary listing, as the legacy page showed nothing then.
    /// </summary>
    IReadOnlyList<EisCampusListing>? GetCampusListings(string employeeId, string? mothraId);
}

/// <summary>
/// Queries ldap.ucdavis.edu with the same service account and settings as <see cref="LdapService"/>,
/// asking for the address attributes that service doesn't load.
/// </summary>
[SupportedOSPlatform("windows")]
[ExcludeFromCodeCoverage(Justification = "Only talks to the campus directory; the listing rules are tested in EisCampusListingsTests.")]
public class EisDirectoryService : IEisDirectoryService
{
    private const string LdapUser = "UID=vetmed,OU=Special Users,DC=ucdavis,DC=edu";
    private const string LdapServer = "ldap.ucdavis.edu";
    private const string PeopleBase = "OU=People,DC=ucdavis,DC=edu";
    private const int LdapSslPort = 636;

    private readonly ILogger<EisDirectoryService> _logger;

    public EisDirectoryService(ILogger<EisDirectoryService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<EisCampusListing>? GetCampusListings(string employeeId, string? mothraId)
    {
        try
        {
            using LdapConnection connection = Connect();
            List<Dictionary<string, string>> primary = Search(
                connection, $"(employeeNumber={LdapFilter.Escape(employeeId)})");
            if (primary.Count == 0)
            {
                return [];
            }

            List<EisCampusListing> listings = [EisCampusListings.Create(primary[0], isPrimary: true)];
            if (!string.IsNullOrWhiteSpace(mothraId))
            {
                // Additional listings share the person's UUID but are not listing number 1.
                listings.AddRange(Search(
                        connection, $"(&(ucdPersonUUID={LdapFilter.Escape(mothraId)})(!(ucdListingOrder=1)))")
                    .OrderBy(EisCampusListings.ListingOrder)
                    .Select(attributes => EisCampusListings.Create(attributes, isPrimary: false)));
            }

            return listings;
        }
        catch (LdapException ex)
        {
            _logger.LogWarning(ex, "Campus directory unavailable for the EIS address page");
            return null;
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogWarning(ex, "Campus directory query failed for the EIS address page");
            return null;
        }
    }

    private static LdapConnection Connect()
    {
        string password = HttpHelper.GetSetting<string>("Credentials", "UCDavisDirectoryLDAP") ?? string.Empty;
        var connection = new LdapConnection(
            new LdapDirectoryIdentifier(LdapServer, LdapSslPort),
            new NetworkCredential(LdapUser, password),
            AuthType.Basic);
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.SecureSocketLayer = true;
        connection.Bind();
        return connection;
    }

    private static List<Dictionary<string, string>> Search(LdapConnection connection, string filter)
    {
        var request = new SearchRequest(PeopleBase, filter, SearchScope.OneLevel, [.. EisCampusListings.Attributes]);
        var response = (SearchResponse)connection.SendRequest(request);
        return [.. response.Entries.Cast<SearchResultEntry>().Select(ToAttributes)];
    }

    private static Dictionary<string, string> ToAttributes(SearchResultEntry entry)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DirectoryAttribute attribute in entry.Attributes.Values)
        {
            if (attribute.Count > 0 && attribute[0] is { } value)
            {
                attributes[attribute.Name] = value.ToString() ?? string.Empty;
            }
        }

        return attributes;
    }
}
