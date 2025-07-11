using System.Runtime.Versioning;
using Viper.Models.AAUD;

namespace Viper.Areas.Directory.Models
{
    public class UserInfoResultComplete: UserInfoResult
    {

        public UserInfoResultComplete()
        {

        }

        [SupportedOSPlatform("windows")]
        public UserInfoResultComplete(AaudUser? aaudUser, LdapUserContact? ldapUserContact)
            : base(aaudUser, ldapUserContact)
        {
        }
    }
}
