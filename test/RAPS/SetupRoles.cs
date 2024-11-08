using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viper.Models.RAPS;

namespace Viper.test.RAPS
{
    static internal class SetupRoles
    {
        public static readonly List<TblRole> MockRoles = new()
        {
            new TblRole()
            {
                RoleId = 1,
                Role = "VIPER.TestRole",
                Application = 1,
                UpdateFreq = 1,
                AllowAllUsers = true
            },
            new TblRole()
            {
                RoleId = 2,
                Role = "VIPER.AnotherTestRole",
                Application = 0,
                UpdateFreq = 1,
                AllowAllUsers = true
            },
            new TblRole()
            {
                RoleId = 3,
                Role = "VMACS.VMTH.TestVMACSRole",
                Application = 0,
                UpdateFreq = 1,
                AllowAllUsers = false
            }
        };
    }
}
