using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Viper.Areas.Jobs.Model;
using Viper.Classes;

namespace Viper.Areas.Jobs.Controllers
{
    [Area("Jobs")]
    [Route("[area]/[action]")]
    [Authorize(Roles = "VMDO CATS-Programmers")]//, Policy = "2faAuthentication"
    public class JobsController : AreaController
    {
        private readonly ISchedulerFactory schedulerFactory;
        public IUserHelper UserHelper;

        public int Count { get; set; }
        public string? UserName { get; set; }

        public JobsController(Classes.SQLContext.VIPERContext context, IWebHostEnvironment environment, ISchedulerFactory schedulerFactory)
        {
            this.schedulerFactory = schedulerFactory;
            UserHelper = new UserHelper();
        }

        /// <summary>
        /// List of jobs
        /// </summary>
        [Route("/[area]")]
        public async Task<ActionResult> Index()
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var jobGroups = await scheduler.GetJobGroupNames();
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var jobs = new List<IJobDetail>();
            foreach (var key in jobKeys)
            {
                var j = await scheduler.GetJobDetail(key);
                if (j != null)
                {
                    jobs.Add(j);
                }
            }

            List<JobInfo> jobsInfo = new List<JobInfo>();
            foreach (var job in jobs)
            {
                jobsInfo.Add(await GetJobInfo(job, scheduler));
            }

            ViewData["JobsInfo"] = jobsInfo;
            ViewData["JobGroups"] = jobGroups;
            ViewData["JobKeys"] = jobKeys;

            return await Task.Run(() => View("~/Areas/Jobs/Views/JobList.cshtml"));
        }


        /// <summary>
        /// Delete / Pause / Resume a job
        /// </summary>
        /// <param name="action"></param>
        /// <param name="jobGroup"></param>
        /// <param name="jobKey"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/[area]")]
        public async Task<ActionResult> Index(string action, string jobGroup, string jobKey)
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var jk = new JobKey(jobKey, jobGroup);
            switch (action)
            {
                case "Delete":
                    await scheduler.DeleteJob(jk); break;
                case "Pause":
                    await scheduler.PauseJob(jk); break;
                case "Resume":
                    await scheduler.ResumeJob(jk); break;
                case "Run":
                    var jd = await scheduler.GetJobDetail(jk);
                    if (jd != null)
                    {
                        await (!jd.JobDataMap.IsEmpty ? scheduler.TriggerJob(jk, jd.JobDataMap) : scheduler.TriggerJob(jk));
                    }
                    break;
            }
            return await Task.Run(() => Redirect(string.Format("~/Jobs")));
        }

        /// <summary>
        /// Form to add a job
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult> AddJob()
        {
            var scheduler = await schedulerFactory.GetScheduler();
            ViewData["JobTypes"] = JobRegister.JobTypes;
            return await Task.Run(() => View("~/Areas/Jobs/Views/Job.cshtml"));
        }

        /// <summary>
        /// Handle adding job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> AddJob(AddUpdateJob job, string[] keys, string[] values)
        {
            var scheduler = await schedulerFactory.GetScheduler();
            await ScheduleJob(job, scheduler, keys, values);
            return await Task.Run(() => View("~/Areas/Jobs/Views/Job.cshtml"));
        }

        /// <summary>
        /// Edit a job
        /// </summary>
        /// <param name="JobKey"></param>
        /// <param name="JobGroup"></param>
        /// <returns></returns>
        public async Task<ActionResult> EditJob(string JobKey, string JobGroup)
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var job = await scheduler.GetJobDetail(new Quartz.JobKey(JobKey, JobGroup));

            if (job == null)
            {
                return await Task.Run(() => Redirect(string.Format("~/Jobs")));
            }

            ViewData["JobGroups"] = await scheduler.GetJobGroupNames();
            ViewData["JobTypes"] = JobRegister.JobTypes;
            ViewData["Job"] = await GetJobInfo(job, scheduler);
            return await Task.Run(() => View("~/Areas/Jobs/Views/Job.cshtml"));
        }

        /// <summary>
        /// Update a job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> EditJob(AddUpdateJob job, string[] keys, string[] values, string prevJobKey, string prevJobGroup)
        {
            var scheduler = await schedulerFactory.GetScheduler();
            var exists = await scheduler.GetJobDetail(new JobKey(prevJobKey, prevJobGroup));

            if (exists == null)
            {
                return await Task.Run(() => Redirect(string.Format("~/Jobs")));
            }

            //delete and recreated
            await scheduler.DeleteJob(new JobKey(prevJobKey, prevJobGroup));
            await ScheduleJob(job, scheduler, keys, values);
            return await Task.Run(() => Redirect("~/Jobs/"));
        }

        /// <summary>
        /// Create and schedule a job
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scheduler"></param>
        private async Task ScheduleJob(AddUpdateJob job, IScheduler scheduler, string[] keys, string[] values)
        {
            var jobKey = new JobKey(job.JobKey, job.JobGroup);
            var builder = JobRegister.GetJobBuilder(jobKey, job.JobType);
            var jobTrigger = TriggerBuilder.Create()
                .WithIdentity($"trigger-for-{job.JobGroup}-{job.JobKey}", "job-triggers")
                .WithDescription(job.TimingDescription ?? "")
                .ForJob(jobKey)
                .StartAt(DateTimeOffset.Now.AddSeconds(5))
                .WithCronSchedule(job.CronExpression)
                .Build();

            JobDataMap map = new();
            for (var i = 0; i < 10; i++)
            {
                if (keys[i] != null && values[i] != null)
                {
                    map.Add(keys[i], values[i]);
                }
            }
            builder = builder
                .WithIdentity(jobKey)
                .WithDescription(job.JobDescription ?? "");
            if(!map.IsEmpty)
            {
                builder = builder.UsingJobData(map);
            }
            await scheduler.ScheduleJob(builder.Build(), jobTrigger);
        }

        /// <summary>
        /// Get info about the job to display
        /// </summary>
        /// <param name="job"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        private async Task<JobInfo> GetJobInfo(IJobDetail job, IScheduler scheduler)
        {
            JobInfo j = new()
            {
                JobKey = job.Key.Name,
                JobGroup = job.Key.Group,
                JobDescription = job.Description,
                JobClassName = job.JobType.ToString().Split(".").Last(),
                Parameters = job.JobDataMap
            };

            var triggers = await scheduler.GetTriggersOfJob(job.Key);
            var trigger = triggers.Where(t =>
            {
                var st = t as ISimpleTrigger ?? null;
                return st == null || st.RepeatCount != 0;
            }).FirstOrDefault();
            if (trigger != null)
            {
                j.JobState = await scheduler.GetTriggerState(trigger.Key);
                j.TriggerDescription = trigger.Description;
                j.JobStartTime = trigger.StartTimeUtc.ToLocalTime();
                j.NextRunTime = trigger.GetNextFireTimeUtc().HasValue ? trigger.GetNextFireTimeUtc()!.Value.ToLocalTime() : null;
                j.TimingDescription = trigger.Description;
                var cronTrigger = trigger as ICronTrigger ?? null;
                if (cronTrigger != null)
                {
                    j.CronExpression = cronTrigger.CronExpressionString;
                }
                else
                {
                    var simpleTrigger = trigger as ISimpleTrigger ?? null;
                    if (simpleTrigger != null && j.TimingDescription == null)
                    {
                        j.TimingDescription = $"Repeat {(simpleTrigger.RepeatCount == -1 ? "Forever" : simpleTrigger.RepeatCount + "Times")} every {simpleTrigger.RepeatInterval} ";
                    }
                }
            }
            return j;
        }
    }
}
