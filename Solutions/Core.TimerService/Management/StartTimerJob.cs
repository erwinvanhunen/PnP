using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.TimerService.Domain;
using OfficeDevPnP.TimerService.Enums;
using Job = OfficeDevPnP.TimerService.Domain.Job;

namespace OfficeDevPnP.TimerService.Management
{
    [Cmdlet(VerbsLifecycle.Start, "SPOTimerJob")]
    public class StartTimerJob : BaseCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name;

        [Parameter(Mandatory = true)]
        public ScheduleType ScheduleType;

        [Parameter(Mandatory = true)]
        public AuthenticationType AuthenticationType;

        [Parameter(Mandatory = false)]
        public string Credential;

        [Parameter(Mandatory = false)]
        public string Username;

        [Parameter(Mandatory = false)]
        public string Password;

        [Parameter(Mandatory = false)]
        public string Domain;

        [Parameter(Mandatory = false)]
        public int Interval;

        [Parameter(Mandatory = false)]
        public string Time;

        [Parameter(Mandatory = false)]
        public byte Day;

        [Parameter(Mandatory = false)]
        public bool Disabled;

        [Parameter(Mandatory = true)]
        public List<string> Sites; 

        protected override void ExecuteCmdlet()
        {
            Job job = new Job();
            job.Name = Name;
            job.ScheduleType = ScheduleType;
            job.AuthenticationType = AuthenticationType;
            job.Interval = Interval;
            job.Username = Username;
            job.Password = Password;
            job.Domain = Domain;
            job.Time = Time;
            job.Day = Day;
            job.Disabled = Disabled;

            foreach (var site in Sites)
            {
                job.Sites.Add(new JobSite() {Url = site});
            }

            jobs.JobCollection.Add(job);
            SaveJobs();
        }
    }
}
