using System.Runtime.Versioning;
using System.DirectoryServices.Protocols;
using System.Text;
using Viper.Areas.Directory.Models;

namespace Viper.Classes.Utilities
{
    [SupportedOSPlatform("windows")]
    public static class LdapService
    {
        private const string _ldapUsername = "UID=vetmed,OU=Special Users,DC=ucdavis,DC=edu";
        private const string _ldapServer = "ldap.ucdavis.edu";
        private const string _ldapStart = "OU=People,DC=ucdavis,DC=edu";
        private const int _ldapSSLPort = 636;

        private static readonly string[] personAttributes =
        {
            "givenName","sn","middlename","ou","mail","telephoneNumber","mobile","postalAddress",
            "ucdStudentLevel","labeledUri","title","uid","ucdPersonPIDM","ucdPersonIAMID","employeeNumber",
            "ucdStudentSID","ucdPersonUUID","eduPersonNickname","ucdPersonAffiliation","displayname"
        };

        private static SearchResponse SearchLdap(string searchFilter)
        {
            var ldapIdentifier = new LdapDirectoryIdentifier(_ldapServer, _ldapSSLPort);
            var cred = HttpHelper.GetSetting<string>("Credentials", "UCDavisDirectoryLDAP") ?? "";
            using var lc = new LdapConnection(ldapIdentifier,
                    new System.Net.NetworkCredential(_ldapUsername, cred),
                    AuthType.Basic);
            lc.SessionOptions.ProtocolVersion = 3;
            lc.SessionOptions.SecureSocketLayer = true;
            lc.SessionOptions.VerifyServerCertificate = (connection, certificate) => true;
            lc.Bind();

            var searchRequest = new SearchRequest(_ldapStart, searchFilter, SearchScope.Subtree, personAttributes);
            var response = (SearchResponse)lc.SendRequest(searchRequest);
            return response;
        }

        /// <summary>
        /// Get users for display\ searching by name, cn, or samAccountName
        /// </summary>
        /// <param name="search">Searches all fields (phone number, SN, given name, UID, CN, mail) for this value</param>
        /// <returns>List of Users</returns>
        public static List<LdapUserContact> GetUsersContact(string search)
        {
            List<LdapUserContact> users = new();
            string filter = string.Format("(|(telephoneNumber=*{0})(sn={0}*)(givenName={0}*)(uid={0}*)(cn={0})(mail={0}*))", search);
            var results = SearchLdap(filter);

            foreach (SearchResultEntry entry in results.Entries)
            {
                users.Add(new LdapUserContact(entry));
                if (users.Count >= 100)
                {
                    break;
                }
            }
            return SortUsersContact(users);
        }

        /// <summary>
        /// Get users for display, optionally searching ou.ad3, searching by name, cn, or samAccountName
        /// </summary>
        /// <param name="search">Searches all fields (phone number, SN, given name, UID, CN, mail) for this value</param>
        /// <returns>Dictionary of Users, indexed by mothraID</returns>
        public static Dictionary<string, LdapUserContact> GetUsersContactDictionary(string search)
        {
            Dictionary<string, LdapUserContact> users = new();
            string filter = string.Format("(|(telephoneNumber=*{0})(sn={0}*)(givenName={0}*)(uid={0}*)(cn={0})(mail={0}*))", search);
            var results = SearchLdap(filter);
            foreach (SearchResultEntry entry in results.Entries)
            {
                var user = new LdapUserContact(entry);
                if (user.MothraId != null)
                {
                    users.Add(user.MothraId, user);
                }
            }
            return users;
        }

        /// <summary>
        /// Look up User by search string
        /// </summary>
        /// <param name="search">Search string for looking up user</param>
        /// <returns>LdapUserContact</returns>
        public static LdapUserContact? GetUserContact(string search)
        {
            string filter = string.Format("(|(telephoneNumber=*{0})(sn={0}*)(givenName={0}*)(uid={0}*)(cn={0})(mail={0}*))", search);
            var results = SearchLdap(filter);

            if (results.Entries.Count > 0)
            {
                return new LdapUserContact(results.Entries[0]);
            }
            return null;
        }


        /// <summary>
        /// Look up User by its iamID
        /// </summary>
        /// <param name="id">iamID for looking up user</param>
        /// <returns>LdapUserContact</returns>
        public static LdapUserContact? GetUserByID(string? id)
        {
            if (id == null) return null;
            string filter = string.Format("(ucdpersoniamid = {0})", id);
            var results = SearchLdap(filter);
            if (results.Entries.Count > 0)
            {
                return new LdapUserContact(results.Entries[0]);
            }
            return null;
        }

        /// <summary>
        /// Get dictionary of Users from a list of MothraIDs
        /// </summary>
        /// <param name="ids">List of MothraIDs for looking up users</param>
        /// <returns>Dictionary of Users, indexed by mothraID</returns>
        public static Dictionary<string, LdapUserContact> GetUsersByIDs(List<string> ids)
        {
            var filterBuilder = new StringBuilder("(|");
            foreach (string i in ids)
            {
                filterBuilder.Append($"(ucdpersonuuid = {i})");
            }
            filterBuilder.Append(')');
            string filter = filterBuilder.ToString();
            var results = SearchLdap(filter);
            var users = new Dictionary<string, LdapUserContact>();
            foreach (SearchResultEntry entry in results.Entries)
            {
                var user = new LdapUserContact(entry);
                if (user.MothraId != null)
                {
                    users.Add(user.MothraId, user);
                }
            }
            return users;
        }

        /// <summary>
        /// Gets a single user from ou or ad3
        /// </summary>
        /// <param name="samAccountName">samAccountName of user</param>
        /// <param name="fromOu">If true, searches the SVM OU in ou.ad3.ucdavis.edu, otherwise, searches ucdUsers in ad3</param>
        /// <returns></returns>
        public static LdapUserContact? GetUserBySamAccountName(string? samAccountName)
        {
            string filter = string.Format("(&(objectClass=user)(samAccountName={0}))", samAccountName);
            var results = SearchLdap(filter);
            if (results.Entries.Count > 0)
            {
                return new LdapUserContact(results.Entries[0]);
            }
            return null;
        }

        //Sort users by description first, or sam account name
        private static List<LdapUserContact> SortUsersContact(List<LdapUserContact> users)
        {
            users.Sort((a, b) => a.DisplayName == b.DisplayName
                ? a.Sn.CompareTo(b.Sn)
                : a.DisplayName.CompareTo(b.DisplayName));
            return users;
        }
    }
}
