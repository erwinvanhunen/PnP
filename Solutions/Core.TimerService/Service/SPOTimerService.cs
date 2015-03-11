using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using OfficeDevPnP.Core.Utilities;
using OfficeDevPnP.TimerService.Domain;
using OfficeDevPnP.TimerService.Enums;
using OfficeDevPnP.TimerService.Helpers;
using Timer = System.Timers.Timer;

namespace OfficeDevPnP.TimerService
{
    public partial class SPOTimerService : ServiceBase
    {
        private string _configFile;
        private const string Configfilename = "jobs.xml";
        private readonly Object _timerLock = new Object();
        private Jobs jobs;

        private List<JobRunner> _jobQueue;


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
                jobs = new Jobs();

                var ser = new XmlSerializer(typeof(Jobs));
                using (TextWriter writer = new StreamWriter(_configFile))
                {
                    ser.Serialize(writer, jobs);
                }

            }
            else
            {
                var xml = File.ReadAllText(_configFile);
                jobs = xml.ParseXML<Jobs>();
              
                foreach (var job in jobs.JobCollection)
                {
                    if (job.IsRunning)
                    {
                        job.IsRunning = false;
                    }
                }

                SaveJobs();

            }

            var thread = new Thread(() =>
            {
                // Wait until the exact minute to start
                Thread.Sleep((60 - DateTime.Now.Second)*1000);
                ParseJobConfig();

                // Setup a timer to start every 60 seconds;
                var timer = new Timer {Interval = 60000};
                timer.Elapsed += TimerOnElapsed;
                timer.Start();
            });
            thread.Start();
        }

        private void SaveJobs()
        {
            // Check Passwords
            foreach (var job in jobs.JobCollection)
            {
                if (job.Password != null)
                {
                    var decryptedPw = Encryption.DecryptString(job.Password);

                    if (decryptedPw.Length == 0)
                    {
                        job.Password = Encryption.EncryptString(Encryption.ToSecureString(job.Password));
                    }
                }
            }
            XmlSerializer ser = new XmlSerializer(typeof(Jobs));
            using (TextWriter writer = new StreamWriter(_configFile))
            {
                ser.Serialize(writer, jobs);
            }
        }

        private Jobs LoadJobs()
        {
            var xml = File.ReadAllText(_configFile);
            return xml.ParseXML<Jobs>();
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

            jobs = LoadJobs();

            if (jobs.JobCollection.Any())
            {
                Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.EnumeratingJobs);
                foreach (var job in jobs.JobCollection)
                {
                    if (!job.IsRunning && !job.Disabled)
                    {
                        // Check if ID set, if not, add ID
                        if (job.Id == Guid.Empty)
                        {
                            job.Id = Guid.NewGuid();
                        }
                        SaveJobs();

                        switch (job.ScheduleType)
                        {
                            case ScheduleType.Minute:
                                {
                                    ParseMinuteJob(job);
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
                    if (jobs.UseThreading)
                    {
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
                    else
                    {
                        runner.RunJob();
                    }
                }
            }
        }

        public void OnTaskBeginning(JobRunner jobRunner)
        {
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.StartingJob0_1, jobRunner.Job.Name, jobRunner.Job.Id);

            jobRunner.Job.IsRunning = true;


            lock (_timerLock)
            {
                SaveJobs();
            }


        }

        public void OnTaskComplete(JobRunner jobRunner)
        {
            jobRunner.Job.IsRunning = false;

            lock (_timerLock)
            {
                SaveJobs();
            }

            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.FinishedJob0_1, jobRunner.Job.Name, jobRunner.Job.Id);
        }

        private void ParseWeeklyJob(Job job)
        {
            DateTime specificTime;

            DateTime.TryParse(job.Time, out specificTime);

            var now = DateTime.Now;
            if (((int)now.DayOfWeek) == job.Day && now.Hour == specificTime.Hour && now.Minute == specificTime.Minute)
            {
                QueueJob(job);
            }
        }

        private void ParseDailyJob(Job job)
        {
            DateTime specificTime;

            DateTime.TryParse(job.Time, out specificTime);

            var now = DateTime.Now;

            if (now.Hour == specificTime.Hour && now.Minute == specificTime.Minute)
            {
                QueueJob(job);
            }
        }

        private void ParseMinuteJob(Job job)
        {
            if (job.LastRun > 0)
            {
                TimeSpan timeSpan = new TimeSpan(DateTime.Now.Ticks);
                var difference = timeSpan.Subtract(new TimeSpan(job.LastRun));
                // Check how long ago it is
                if (difference.TotalMinutes >= job.Interval)
                {
                    QueueJob(job);
                }
            }
            else
            {
                QueueJob(job);
            }

        }

        private void QueueJob(Job job)
        {
            if (job.Sites.Any())
            {
                var jobRunner = new JobRunner(ref job);

                _jobQueue.Add(jobRunner);

            }

        }
        protected override void OnStop()
        {
            Log.Info(ApplicationStrings.ServiceIndentifier, ApplicationStrings.StoppingService);
        }
    }
}
