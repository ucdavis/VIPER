using Quartz;
using Viper.Areas.Jobs.JobClasses;

namespace Viper.Areas.Jobs.Model
{
    public class JobRegister
    {
        public static readonly List<string> JobTypes = new()
        {
            "RapsRoleViewUpdate"
        };

        /// <summary>
        /// Create Job Detail, using the string type to set the class to invoke
        /// </summary>
        /// <param name="jobKey"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static JobBuilder GetJobBuilder(JobKey jobKey, string type)
        {
            return type switch
            {
                "RapsRoleViewUpdate" => JobBuilder.Create<RapsRoleViewUpdate>(),
                _ => throw new Exception("Invalid type"),
            };
        }
    }
}
