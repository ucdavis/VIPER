using System;
using System.Linq;

namespace Viper.Areas.Effort.Scripts
{
    /// <summary>
    /// Entry point for Effort System data scripts
    /// Routes to either EffortDataAnalysis or EffortDataRemediation based on command line args
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // Check if first arg is "EffortDataRemediation" or starts with remediation flags
            bool isRemediation = args.Length > 0 &&
                (args[0].Equals("EffortDataRemediation", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("remediation", StringComparison.OrdinalIgnoreCase) ||
                 args[0].Equals("remediate", StringComparison.OrdinalIgnoreCase));

            if (isRemediation)
            {
                // Remove the "EffortDataRemediation" arg and pass the rest
                var remediationArgs = args.Skip(1).ToArray();
                EffortDataRemediation.Run(remediationArgs);
            }
            else
            {
                // Default to analysis
                EffortDataAnalysis.Run(args);
            }
        }
    }
}
