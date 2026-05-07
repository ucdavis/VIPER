using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.Versioning;
using Viper.Areas.RAPS.Models;

namespace Viper.Classes.Utilities
{
    [SupportedOSPlatform("windows")]
    public class ActiveDirectoryService
    {
        //username for campus active directory servers (ou.ad3 and ad3)
        private const string _username = "svmadgrp";

        //Start OUs for ou.ad3 (old-style groups and service accounts) and ad3 (users and api managed groups)
        private const string _ouStart = "OU=SVM,OU=DEPARTMENTS,DC=ou,DC=ad3,DC=ucdavis,DC=edu";
        private const string _ad3Users = "OU=ucdUsers,DC=ad3,DC=ucdavis,DC=edu";
        //server and start for ldap.ucdavis.edu
        private const string _ouServer = "ou.ad3.ucdavis.edu";
        private const string _ad3Server = "ad3.ucdavis.edu";

        private const int port = 636;
        //ldap attributes/properties to return for each object type
        private static readonly string[] _groupProperties =
        {
            "sAMAccountName",
            "objectGuid",
            "cn",
            "canonicalName",
            "dn",
            "distinguishedName",
            "displayName",
            "groupType",
            "description",
            "extensionAttribute1",
            "extensionAttribute2",
            "extensionAttribute3"
        };
        private static readonly string[] _personProperties =
        {
            "sAMAccountName",
            "objectGuid",
            "cn",
            "canonicalName",
            "dn",
            "distinguishedName",
            "givenName",
            "sn",
            "displayName",
            "description",
            "mail",
            "streetAddress",
            "userPrincipalName",
            "userAccountControl",
            "title",
            "department",
            "company",
            "uidNumber",
            "memberOf",
            "telephoneNumber",
            "mobile",
            "uid",
            "employeeNumber",
            "labeledUri",
            "middlename",
            "ou",
            "postalAddress",
            "ucdPersonAffiliation",
            "ucdPersonIAMID",
            "ucdPersonPIDM",
            "ucdPersonUUID",
            "ucdStudentLevel",
            "ucdStudentSID"
        };

        private enum Server
        {
            AD3,
            OU
        }

        private enum ObjectType
        {
            Person,
            Group
        }

        /// <summary>
        /// Get groups, optionally filtering with a wildcard match to name
        /// </summary>
        /// <param name="name">Partial name of group to search for</param>
        /// <returns>List of groups</returns>
        public static List<LdapGroup> GetGroups(string? name = null)
        {
            string filter = BuildGroupsFilter(name);

            var searchResults = SearchActiveDirectory(filter, Server.OU, ObjectType.Group);
            List<LdapGroup> groups = new();

            foreach (SearchResultEntry e in searchResults)
            {
                groups.Add(new LdapGroup(e));
            }

            groups.Sort((g1, g2) => g1.DistinguishedName.CompareTo(g2.DistinguishedName));
            return groups;
        }

        /// <summary>
        /// Get a single group by distinguished name
        /// </summary>
        /// <param name="dn">Distinguished name of group</param>
        /// <returns>The group, if found</returns>
        public static LdapGroup? GetGroup(string dn)
        {
            string? filter = BuildGroupFilter(dn);
            if (filter == null)
            {
                return null;
            }
            var searchResults = SearchActiveDirectory(filter, Server.OU, ObjectType.Group);
            LdapGroup? group = null;
            if (searchResults.Count == 1)
            {
                group = new LdapGroup(searchResults[0]);
            }

            return group;
        }

        /// <summary>
        /// Get users, optionally searching ou.ad3, searching by name, cn, or samAccountName
        /// </summary>
        /// <param name="fromOu">If true, searches the SVM OU in ou.ad3.ucdavis.edu, otherwise, searches ucdUsers in ad3</param>
        /// <param name="name">display name to search for</param>
        /// <param name="cn">canonical name to search for</param>
        /// <param name="samAccountName">samAccountName to search for</param>
        /// <returns>List of users</returns>
        public static List<LdapUser> GetUsers(bool fromOu = false, string? name = null, string? cn = null, string? samAccountName = null)
        {
            string filter = BuildUsersFilter(name, cn, samAccountName);

            var searchResults = SearchActiveDirectory(filter, fromOu ? Server.OU : Server.AD3, ObjectType.Person);

            List<LdapUser> users = new();
            foreach (SearchResultEntry result in searchResults)
            {
                users.Add(new LdapUser(result));
            }

            return SortUsers(users);
        }

        /// <summary>
        /// Gets a single user from ou or ad3
        /// </summary>
        /// <param name="samAccountName">samAccountName of user</param>
        /// <param name="fromOu">If true, searches the SVM OU in ou.ad3.ucdavis.edu, otherwise, searches ucdUsers in ad3</param>
        public static LdapUser? GetUser(string samAccountName, bool fromOu = false)
        {
            string? filter = BuildUserFilter(samAccountName);
            if (filter == null)
            {
                return null;
            }
            var searchResults = SearchActiveDirectory(
                filter,
                fromOu ? Server.OU : Server.AD3,
                ObjectType.Person);
            return searchResults.Count == 1 ? new LdapUser(searchResults[0]) : null;
        }

        /// <summary>
        /// Get members of a group. Group must be in ou.ad3. Members can be in either ou.ad3 or ad3
        /// </summary>
        /// <param name="groupDn">Distingushed name of the group</param>
        /// <returns>List of members of the group</returns>
        public static List<LdapUser> GetGroupMembership(string groupDn)
        {
            string? filter = BuildGroupMembershipFilter(groupDn);
            if (filter == null)
            {
                return new List<LdapUser>();
            }
            List<LdapUser> users = new();
            ////Need to get users from both AD3 and OU
            var ouSearchResults = SearchActiveDirectory(filter, Server.OU, ObjectType.Person);
            var ad3SearchResults = SearchActiveDirectory(filter, Server.AD3, ObjectType.Person);

            foreach (SearchResultEntry result in ouSearchResults)
            {
                users.Add(new LdapUser(result));
            }
            foreach (SearchResultEntry result in ad3SearchResults)
            {
                users.Add(new LdapUser(result));
            }

            return SortUsers(users);
        }

        /// <summary>
        /// Add a user to a group
        /// </summary>
        /// <param name="userDn">The distinguished name of the user</param>
        /// <param name="groupDn">The distinguised name of the group</param>
        public static void AddUserToGroup(string userDn, string groupDn)
        {
            string creds = HttpHelper.GetSetting<string>("Credentials", "svmadgrp") ?? "";
            try
            {
                using PrincipalContext ad3Pc = new(ContextType.Domain, "ad3.ucdavis.edu", _username, creds);
                using PrincipalContext ouPc = new(ContextType.Domain, "ou.ad3.ucdavis.edu", _username, creds);
                GroupPrincipal? group = GroupPrincipal.FindByIdentity(ouPc, IdentityType.DistinguishedName, groupDn);
                UserPrincipal? user = UserPrincipal.FindByIdentity(ad3Pc, IdentityType.DistinguishedName, userDn);
                if (group != null && user != null)
                {
                    group.Members.Add(user);
                    group.Save();
                }
            }
            catch (Exception ex)
            {
                HttpHelper.Logger.Error(ex);
            }
        }

        /// <summary>
        /// Remove a user from a group
        /// </summary>
        /// <param name="userDn">The distinguished name of the user</param>
        /// <param name="groupDn">The distinguised name of the group</param>
        public static void RemoveUserFromGroup(string userDn, string groupDn)
        {
            string creds = HttpHelper.GetSetting<string>("Credentials", "svmadgrp") ?? "";
            try
            {
                using PrincipalContext ad3Pc = new(ContextType.Domain, "ad3.ucdavis.edu", _username, creds);
                using PrincipalContext ouPc = new(ContextType.Domain, "ou.ad3.ucdavis.edu", _username, creds);
                GroupPrincipal group = GroupPrincipal.FindByIdentity(ouPc, IdentityType.DistinguishedName, groupDn);
                UserPrincipal? user = UserPrincipal.FindByIdentity(ad3Pc, IdentityType.DistinguishedName, userDn);
                if (group != null && user != null)
                {
                    group.Members.Remove(user);
                    group.Save();
                }
            }
            catch (Exception ex)
            {
                HttpHelper.Logger.Error(ex);
            }
        }

        internal static string BuildGroupsFilter(string? name)
        {
            const string baseFilter = "(objectClass=group)";
            if (string.IsNullOrWhiteSpace(name))
            {
                return baseFilter;
            }
            return string.Format("(&{0}(cn=*{1}*))", baseFilter, LdapFilter.Escape(name));
        }

        internal static string? BuildGroupFilter(string? dn)
        {
            if (string.IsNullOrWhiteSpace(dn))
            {
                return null;
            }
            return string.Format("(&(objectClass=group)(distinguishedName={0}))", LdapFilter.Escape(dn));
        }

        internal static string? BuildUserFilter(string? samAccountName)
        {
            if (string.IsNullOrWhiteSpace(samAccountName))
            {
                return null;
            }
            return string.Format("(&(objectClass=user)(samAccountName={0}))", LdapFilter.Escape(samAccountName));
        }

        internal static string? BuildGroupMembershipFilter(string? groupDn)
        {
            if (string.IsNullOrWhiteSpace(groupDn))
            {
                return null;
            }
            return string.Format("(&(objectClass=user)(memberOf={0}))", LdapFilter.Escape(groupDn));
        }

        internal static string BuildUsersFilter(string? name, string? cn, string? samAccountName)
        {
            const string baseFilter = "(objectClass=user)";
            string additionalFilters = "";
            if (!string.IsNullOrWhiteSpace(name))
            {
                additionalFilters += string.Format("(displayName=*{0}*)", LdapFilter.Escape(name));
            }
            if (!string.IsNullOrWhiteSpace(cn))
            {
                additionalFilters += string.Format("(cn=*{0}*)", LdapFilter.Escape(cn));
            }
            if (!string.IsNullOrWhiteSpace(samAccountName))
            {
                additionalFilters += string.Format("(samAccountName=*{0}*)", LdapFilter.Escape(samAccountName));
            }
            if (additionalFilters.Length > 0)
            {
                return string.Format("(&{0}{1})", baseFilter, additionalFilters);
            }
            return baseFilter;
        }

        //Sort users by description first, or sam account name
        private static List<LdapUser> SortUsers(List<LdapUser> users)
        {
            users.Sort((a, b) => a.Description == b.Description
                ? a.SamAccountName.CompareTo(b.SamAccountName)
                : a.Description.CompareTo(b.Description));
            return users;
        }

        /// <summary>
        /// Generic search function
        /// </summary>
        private static List<SearchResultEntry> SearchActiveDirectory(string searchFilter, Server server, ObjectType objectType)
        {
            var ldapIdentifier = server == Server.OU
                ? new LdapDirectoryIdentifier(_ouServer, port)
                : new LdapDirectoryIdentifier(_ad3Server, port);
            var searchStart = server == Server.OU
                ? _ouStart
                : _ad3Users;
            var props = objectType == ObjectType.Group
                ? _groupProperties
                : _personProperties;
            var cred = HttpHelper.GetSetting<string>("Credentials", "svmadgrp") ?? "";
            using var lc = new LdapConnection(ldapIdentifier, new NetworkCredential(_username, cred, "ad3.ucdavis.edu"));
            lc.SessionOptions.ProtocolVersion = 3;
            lc.SessionOptions.SecureSocketLayer = true;
            lc.Bind();

            var searchRequest = new SearchRequest(searchStart, searchFilter, SearchScope.Subtree, props);
            // AD caps a single search at 1000 entries; use paging to retrieve all results.
            var pageRequest = new PageResultRequestControl(1000);
            searchRequest.Controls.Add(pageRequest);

            var entries = new List<SearchResultEntry>();
            while (true)
            {
                var response = (SearchResponse)lc.SendRequest(searchRequest);
                foreach (SearchResultEntry entry in response.Entries)
                {
                    entries.Add(entry);
                }

                var pageResponse = response.Controls
                    .OfType<PageResultResponseControl>()
                    .FirstOrDefault();
                if (pageResponse == null || pageResponse.Cookie.Length == 0)
                {
                    break;
                }
                pageRequest.Cookie = pageResponse.Cookie;
            }
            return entries;
        }
    }
}
