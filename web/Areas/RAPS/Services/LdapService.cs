using System.DirectoryServices;
using System.Runtime.Versioning;
using System.DirectoryServices.AccountManagement;
using Viper.Areas.RAPS.Models;
using NLog;

namespace Viper.Areas.RAPS.Services
{
    [SupportedOSPlatform("windows")]
    public class LdapService
    {
        private const string _username = "ou\\svc-accounts";

        private Logger _logger;

        //Start OUs for ou.ad3 (old-style groups and service accounts) and ad3 (users and api managed groups)
        private const string _ouStart = "OU=SVM,OU=DEPARTMENTS,DC=ou,DC=ad3,DC=ucdavis,DC=edu";
        private const string _ad3Users = "OU=ucdUsers,DC=ad3,DC=ucdavis,DC=edu";
        private const string _ouServer = "ou.ad3.ucdavis.edu";
        private const string _ad3Server = "ad3.ucdavis.edu";

        //ldap attributes/properties to return for each object type
        private readonly string[] _groupProperties =
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
        private readonly string[] _personProperties =
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
            "memberOf"
        };
        
        public LdapService() {
            _logger = LogManager.GetCurrentClassLogger();
        }
        
        /// <summary>
        /// Get groups, optionally filtering with a wildcard match to name
        /// </summary>
        /// <param name="name">Partial name of group to search for</param>
        /// <returns>List of groups</returns>
        public List<LdapGroup> GetGroups(string? name = null)
        {
            string filter = "(objectClass=group)";
            if (name != null)
            {
                filter = string.Format("(&{0}(cn=*{1}*))", filter, name);
            };

            List<LdapGroup> groups = new();
            try
            {
                var de = GetRoot(true);
                if (de.Options != null)
                {
                    _logger.Info("Setting DE Referral");
                    de.Options.Referral = ReferralChasingOption.All;
                }
                var ds = new DirectorySearcher(de, filter, _groupProperties, SearchScope.Subtree)
                    { ReferralChasing = ReferralChasingOption.All };

                SearchResultCollection results = ds.FindAll();
                foreach (SearchResult result in results)
                {
                    groups.Add(new LdapGroup(result));
                }
                groups.Sort((g1, g2) => g1.DistinguishedName.CompareTo(g2.DistinguishedName));
            }
            catch (DirectoryServicesCOMException cex)
            {
                _logger.Error(cex);
                _logger.Error("Extended Error: " + cex?.ExtendedErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return groups;
        }

        /// <summary>
        /// Get a single group by distinguished name
        /// </summary>
        /// <param name="dn">Distinguished name of group</param>
        /// <returns>The group, if found</returns>
        public LdapGroup? GetGroup(string dn)
        {
            string filter = string.Format("(&(objectClass=group)(distinguishedName={0}))", dn);
            return new LdapGroup(
                new DirectorySearcher(GetRoot(true), filter, _groupProperties, SearchScope.Subtree)
                    .FindOne()
            );
        }

        /// <summary>
        /// Get users, optionally searching ou.ad3, searching by name, cn, or samAccountName
        /// </summary>
        /// <param name="fromOu">If true, searches the SVM OU in ou.ad3.ucdavis.edu, otherwise, searches ucdUsers in ad3</param>
        /// <param name="name">display name to search for</param>
        /// <param name="cn">canonical name to search for</param>
        /// <param name="samAccountName">samAccountName to search for</param>
        /// <returns>List of users</returns>
        public List<LdapUser> GetUsers(bool fromOu = false, string? name = null, string? cn = null, string? samAccountName = null)
        {
            string filter = "(objectClass=user)";
            string additionalFilters = "";
            if(name != null)
            {
                additionalFilters += string.Format("(displayName=*{0}*)", name);
            }
            if(cn != null)
            {
                additionalFilters += string.Format("(cn=*{0}*", cn);
            }
            if(samAccountName != null)
            {
                additionalFilters += string.Format("(samAccountName=*{0}*", samAccountName);
            }
            if(additionalFilters.Length > 0)
            {
                filter = string.Format("(&{0}{1})", filter, additionalFilters);
            }
            SearchResultCollection results = new DirectorySearcher(GetRoot(fromOu), filter, _personProperties, SearchScope.Subtree)
                { PageSize = 1000 }
                .FindAll();
            List<LdapUser> users = new();
            foreach(SearchResult result in results)
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
        /// <returns></returns>
        public LdapUser? GetUser(string samAccountName, bool fromOu = false)
        {
            return new LdapUser(
                new DirectorySearcher(GetRoot(fromOu), string.Format("(&(objectClass=user)(samAccountName={0}))", samAccountName), _personProperties, SearchScope.Subtree)
                .FindOne()
            );

        }

        /// <summary>
        /// Get members of a group. Group must be in ou.ad3. Members can be in either ou.ad3 or ad3
        /// </summary>
        /// <param name="groupDn">Distingushed name of the group</param>
        /// <returns>List of members of the group</returns>
        public List<LdapUser> GetGroupMembership(string groupDn)
        {
            List<LdapUser> users = new();
            string filter = string.Format("(&(objectClass=user)(memberOf={0}))", groupDn);
            
            //Need to get users from both AD3 and OU
            SearchResultCollection ouResults = new DirectorySearcher(GetRoot(true), filter, _personProperties, SearchScope.Subtree)
                .FindAll();
            foreach(SearchResult result in ouResults)
            {
                users.Add(new LdapUser(result));
            }

            SearchResultCollection ad3Results = new DirectorySearcher(GetRoot(false), filter, _personProperties, SearchScope.Subtree)
                { PageSize = 1000 }
                .FindAll();
            foreach (SearchResult result in ad3Results)
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
        public void AddUserToGroup(string userDn, string groupDn)
        {
            string creds = HttpHelper.GetSetting<string>("Credentials", "UCDavisLDAP") ?? "";
            try
            {
                using PrincipalContext ad3Pc = new(ContextType.Domain, "ad3.ucdavis.edu", _username, creds);
                using PrincipalContext ouPc = new(ContextType.Domain, "ou.ad3.ucdavis.edu", _username, creds);
                GroupPrincipal? group = GroupPrincipal.FindByIdentity(ouPc, IdentityType.DistinguishedName, groupDn);
                UserPrincipal? user = UserPrincipal.FindByIdentity(ad3Pc, IdentityType.DistinguishedName, userDn);
                if(group != null && user != null)
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
        public void RemoveUserFromGroup(string userDn, string groupDn)
        {
            string creds = HttpHelper.GetSetting<string>("Credentials", "UCDavisLDAP") ?? "";
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

        //Sort users by description first, or sam account name
        private static List<LdapUser> SortUsers(List<LdapUser> users)
        {
            users.Sort((a, b) => a.Description == b.Description
                ? a.SamAccountName.CompareTo(b.SamAccountName)
                : a.Description.CompareTo(b.Description));
            return users;
        }

        //Get the root to start our ldap query - either the SVM OU in ou.ad3.ucdavis.edu for traditional security groups and SVM managed service accounts or 
        //the ucdUsers OU in ad3.ucdavis.edu for campus user accounts
        private DirectoryEntry GetRoot(bool fromOu = false)
        {
            string start = fromOu ? _ouStart : _ad3Users;
            //string server = fromOu ? _ouServer : _ad3Server;
            string creds = HttpHelper.GetSetting<string>("Credentials", "UCDavisLDAP") ?? "";
            DirectoryEntry de = new DirectoryEntry(string.Format("LDAP://{0}", start), _username, creds, AuthenticationTypes.Secure);
            return de;
        }

    }
}
