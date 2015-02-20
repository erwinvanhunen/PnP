using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OfficeDevPnP.TimerService.Enums;

namespace OfficeDevPnP.TimerService.Management
{
    [Cmdlet(VerbsCommon.Get, "SPOTimerJobs")]
    public class GetTimerJobs : BaseCmdlet
    {
        protected override void ProcessRecord()
        {
            List<SPOTimerJob> timerJobs = new List<SPOTimerJob>();
            var config = GetConfig();

            var jobs = config.Descendants("Jobs").Descendants("Job");

            foreach (var job in jobs)
            {
                var name = job.Attribute("Name") != null ? job.Attribute("Name").Value : "<noname>";
                var id = job.Attribute("Id") != null ? Guid.Parse(job.Attribute("Id").Value) : Guid.Empty;
                var isrunning = job.Attribute("IsRunning") != null && bool.Parse(job.Attribute("IsRunning").Value);
                var lastRunDt = DateTime.MinValue;
                var enabled = job.Attribute("Disabled") != null ? !bool.Parse(job.Attribute("Disabled").Value) : true;
                var lastRun = job.Descendants("LastRun").FirstOrDefault();
                if (lastRun != null)
                {
                    lastRunDt = new DateTime(long.Parse(lastRun.Attribute("Value").Value));
                }
                var scheduleType = (ScheduleType)Enum.Parse(typeof(ScheduleType), job.Attribute("ScheduleType").Value);

                var spoTimerJob = new SPOTimerJob() {LastRun = lastRunDt, Title = name, Id = id, IsRunning = isrunning, Enabled = enabled, ScheduleType = scheduleType};

                timerJobs.Add(spoTimerJob);
            }


            WriteObject(timerJobs, true);
        }
    }
}
