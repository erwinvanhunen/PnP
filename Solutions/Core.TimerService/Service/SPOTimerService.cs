﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using System.Xml.Linq;
using System.Xml.Schema;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.Core.Utilities;
using OfficeDevPnP.TimerService.Enums;
using Timer = System.Timers.Timer;

namespace OfficeDevPnP.TimerService
{
    public partial class SPOTimerService : ServiceBase
    {
        private string _configFile;
        private const string Configfilename = "jobs.xml";
        private readonly Object _timerLock = new Object();

        private List<JobRunner> _jobQueue;

        private XDocument _config;

        public SPOTimerService()
        {
            InitializeComponent();
        }



        protected override void OnStart(string[] args)
        {
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.StartingService);

            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP"));
            }
            _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP", Configfilename);
            if (!File.Exists(_configFile))
            {
                XDocument config = new XDocument();
                config.Add(new XElement("Jobs"));
                config.Save(_configFile);
            }
            else
            {
                // Clear the IsRunning status if present on a job.
                _config = XDocument.Load(_configFile);

                var jobs = _config.Descendants("Jobs").Descendants("Job");
                bool dirty = false;
                foreach (var job in jobs)
                {
                    var isRunning = job.Attribute("IsRunning") != null;
                    if (isRunning)
                    {
                        dirty = true;
                        job.Attribute("IsRunning").Remove();
                    }
                }
                if (dirty)
                {
                    _config.Save(_configFile);
                }

            }

            //ParseJobConfig();

            var timer = new Timer { Interval = 60000 };
            // 60 seconds
            timer.Elapsed += TimerOnElapsed;
            timer.Start();

        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            ParseJobConfig();
        }

        public delegate void OnJobCompleteDelegate(JobRunner jobRunner);

        public delegate void OnJobBeginningDelegate(JobRunner jobRunner);

        private void ParseJobConfig()
        {
            _jobQueue = new List<JobRunner>();

            _config = XDocument.Load(_configFile);


            var jobs = _config.Descendants("Jobs").Descendants("Job");
            if (jobs.Any())
            {
                Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.EnumeratingJobs);
                foreach (var job in jobs)
                {
                    var jobDisabled = job.Attribute("Disabled") != null ? bool.Parse(job.Attribute("Disabled").Value) : false;

                    var isRunning = job.Attribute("IsRunning") != null;
                    if (!isRunning && !jobDisabled)
                    {
                        // Check if ID set, if not, add ID
                        var jobId = job.Attribute("Id") != null ? job.Attribute("Id").Value : null;
                        if (jobId == null)
                        {
                            job.SetAttributeValue("Id", Guid.NewGuid());
                        }
                        var scheduleType = (ScheduleType)Enum.Parse(typeof(ScheduleType), job.Attribute("ScheduleType").Value);
                        var lastRun = job.Descendants("LastRun").FirstOrDefault();
                        switch (scheduleType)
                        {
                            case ScheduleType.Minute:
                                {
                                    ParseMinuteJob(job, lastRun);
                                    break;
                                }
                            case ScheduleType.Daily:
                                {
                                    ParseDailyJob(job);
                                    break;
                                }
                            case ScheduleType.Weekly:
                                {
                                    ParseWeeklyJob(job);
                                    break;
                                }
                        }
                    }
                }

                var jobBeginningDelegate = new OnJobBeginningDelegate(OnTaskBeginning);
                var jobFinishedCallback = new OnJobCompleteDelegate(OnTaskComplete);


                foreach (var jobRunner in _jobQueue)
                {
                    var runner = jobRunner;
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        jobBeginningDelegate(runner);

                        runner.RunJob();
                        if (runner.Exception != null)
                        {
                            Log.Error("SPOTIMERSERVICE", runner.Exception.Message);
                        }
                        jobFinishedCallback(runner);
                    });
                }
            }
        }

        public void OnTaskBeginning(JobRunner jobRunner)
        {
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.StartingJob0_1, jobRunner.Name, jobRunner.Id);
            var jobElement = _config.Descendants("Jobs").Descendants("Job").FirstOrDefault(n => n.Attribute("Id").Value == jobRunner.Id.ToString());

            if (jobElement != null)
            {
                var isRunning = jobElement.Attribute("IsRunning") != null;
                if (!isRunning)
                {
                    jobElement.SetAttributeValue("IsRunning", "True");
                    lock (_timerLock)
                    {
                        _config.Save(_configFile);
                    }
                }
            }

        }

        public void OnTaskComplete(JobRunner jobRunner)
        {
            var jobElement = _config.Descendants("Jobs").Descendants("Job").FirstOrDefault(n => n.Attribute("Id").Value == jobRunner.Id.ToString());

            if (jobElement != null)
            {
                var isRunning = jobElement.Attribute("IsRunning") != null;
                if (isRunning)
                {
                    jobElement.Attribute("IsRunning").Remove();
                }

                var lastRun = jobElement.Descendants("LastRun").FirstOrDefault();
                if (lastRun != null)
                {
                    lastRun.Attribute("Value").Value = jobRunner.LastRun.Ticks.ToString();
                }
                else
                {
                    lastRun = new XElement("LastRun");
                    lastRun.SetAttributeValue("Value", jobRunner.LastRun.Ticks.ToString());
                    jobElement.Add(lastRun);
                }
                lock (_timerLock)
                {
                    _config.Save(_configFile);
                }
            }
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.FinishedJob0_1, jobRunner.Name, jobRunner.Id);
        }

        private void ParseWeeklyJob(XElement job)
        {
            DateTime specificTime;

            DateTime.TryParse(job.Attribute("Time").Value, out specificTime);

            int weekday;
            int.TryParse(job.Attribute("Day").Value, out weekday);

            var now = DateTime.Now;
            if (((int)now.DayOfWeek) == weekday && now.Hour == specificTime.Hour && now.Minute == specificTime.Minute)
            {
                QueueJob(job);
            }
        }

        private void ParseDailyJob(XElement job)
        {
            DateTime specificTime;

            DateTime.TryParse(job.Attribute("Time").Value, out specificTime);

            var now = DateTime.Now;

            if (now.Hour == specificTime.Hour && now.Minute == specificTime.Minute)
            {
                QueueJob(job);
            }
        }

        private void ParseMinuteJob(XElement job, XElement lastRun)
        {
            int minuteInterval;
            int.TryParse(job.Attribute("Interval").Value, out minuteInterval);

            if (lastRun != null)
            {
                long lastRunTicks;
                long.TryParse(lastRun.Attribute("Value").Value, out lastRunTicks);
                if (lastRunTicks > 0)
                {
                    TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks);
                    var difference = timeSpan.Subtract(new TimeSpan(lastRunTicks));
                    // Check how long ago it is
                    if (difference.TotalMinutes >= minuteInterval)
                    {
                        QueueJob(job);
                    }
                }
                else
                {
                    QueueJob(job);
                }
            }
            else
            {
                QueueJob(job);
            }
        }

        private void QueueJob(XElement job)
        {
            var timerName = job.Attribute("Name") != null ? job.Attribute("Name").Value : "<unknown>";
            var jobId = job.Attribute("Id") != null ? Guid.Parse(job.Attribute("Id").Value) : Guid.Empty;

            var assemblyPath = job.Attribute("Assembly") != null ? job.Attribute("Assembly").Value : null;
            if (assemblyPath != null)
            {
                var className = job.Attribute("Class") != null ? job.Attribute("Class").Value : null;
                if (className != null)
                {
                    var authentication = job.Descendants("Authentication").FirstOrDefault();
                    if (authentication != null)
                    {
                        var authType = authentication.Attribute("Type") != null ? authentication.Attribute("Type").Value : null;
                        if (authType != null)
                        {
                            var sites = job.Descendants("Sites").Descendants("Site");
                            if (sites.Any())
                            {
                                var jobRunner = new JobRunner { Id = jobId, Name = timerName, Class = className, Assembly = assemblyPath };
                                switch (authType.ToLower())
                                {
                                    case "office365":
                                        {
                                            jobRunner.AuthenticationType = AuthenticationType.Office365;

                                            if (authentication.Attribute("Credential") != null)
                                            {
                                                jobRunner.CredentialManagerLabel = authentication.Attribute("Credential").Value;
                                            }
                                            else
                                            {
                                                jobRunner.Username = authentication.Attribute("Username").Value;
                                                jobRunner.Password = authentication.Attribute("Password").Value;
                                            }
                                            break;
                                        }
                                    case "apponly":
                                        {
                                            jobRunner.AppId = authentication.Attribute("ClientId").Value;
                                            jobRunner.AppSecret = authentication.Attribute("ClientSecret").Value;
                                            jobRunner.AuthenticationType = AuthenticationType.AppOnly;
                                            // Check if wildcards are being used
                                            var wildcardused = false;
                                            foreach (var site in sites)
                                            {
                                                var url = site.Attribute("Url") != null ? site.Attribute("Url").Value : null;
                                                if (url.IndexOf("*") > -1)
                                                {
                                                    wildcardused = true;
                                                    break;
                                                }
                                            }
                                            if (wildcardused)
                                            {
                                                if (authentication.Attribute("Credential") != null)
                                                {
                                                    jobRunner.CredentialManagerLabel = authentication.Attribute("Credential").Value;
                                                }
                                                else
                                                {
                                                    jobRunner.Username = authentication.Attribute("Username").Value;
                                                    jobRunner.Password = authentication.Attribute("Password").Value;
                                                    jobRunner.Domain = authentication.Attribute("Domain") != null ? authentication.Attribute("Domain").Value : null;
                                                }
                                            }
                                            break;
                                        }
                                    case "networkcredential":
                                        {
                                            jobRunner.AuthenticationType = AuthenticationType.NetworkCredentials;
                                            if (authentication.Attribute("Credential") != null)
                                            {
                                                jobRunner.CredentialManagerLabel = authentication.Attribute("Credential").Value;
                                            }
                                            else
                                            {
                                                jobRunner.Username = authentication.Attribute("Username").Value;
                                                jobRunner.Password = authentication.Attribute("Password").Value;
                                                jobRunner.Domain = authentication.Attribute("Domain").Value;
                                            }
                                            break;
                                        }
                                }


                                foreach (var site in sites)
                                {
                                    var url = site.Attribute("Url").Value;
                                    jobRunner.Sites.Add(url);
                                }


                                _jobQueue.Add(jobRunner);

                            }
                        }
                    }
                }
            }
        }
        protected override void OnStop()
        {
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.StoppingService);
        }
    }
}
