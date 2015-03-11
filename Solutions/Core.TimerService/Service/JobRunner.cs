using System;
using System.Runtime.Serialization;
using OfficeDevPnP.Core.Framework.TimerJobs;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.Core.Utilities;
using OfficeDevPnP.TimerService.Domain;

namespace OfficeDevPnP.TimerService
{
    public class JobRunner
    {
        private Job _job;
        public Exception Exception;

        public JobRunner(ref Job job)
        {
            _job = job;
        }

        public Job Job
        {
            get { return _job; }
            set { _job = value; }
        }

        public void RunJob()
        {
            AppDomain appDomain = null;
            try
            {
                appDomain = AppDomain.CreateDomain("SPOTimerJob_" + _job.Id);

                var jobRunnable = (TimerJob)appDomain.CreateInstanceFromAndUnwrap(_job.Assembly, _job.Class);

                switch (_job.AuthenticationType)
                {
                    case AuthenticationType.Office365:
                        {
                            if (!string.IsNullOrEmpty(_job.Credential))
                            {
                                jobRunnable.UseOffice365Authentication(_job.Credential);
                            }
                            else
                            {
                                jobRunnable.UseOffice365Authentication(_job.Username, _job.InsecurePassword);
                            }
                            break;
                        }
                    case AuthenticationType.AppOnly:
                        {

                            jobRunnable.UseAppOnlyAuthentication(_job.AppId, _job.AppSecret);

                            var wildcardused = false;
                            foreach (var site in _job.Sites)
                            {
                                if (site.Url.IndexOf("*", StringComparison.Ordinal) > -1)
                                {
                                    wildcardused = true;
                                    break;
                                }
                            }
                            if (wildcardused)
                            {
                                if (!string.IsNullOrEmpty(_job.Credential))
                                {
                                    jobRunnable.SetEnumerationCredentials(_job.Credential);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(_job.Domain))
                                    {
                                        jobRunnable.SetEnumerationCredentials(_job.Username, _job.InsecurePassword, _job.Domain);
                                    }
                                    else
                                    {
                                        jobRunnable.SetEnumerationCredentials(_job.Username, _job.InsecurePassword);
                                    }
                                }
                            }
                            break;
                        }
                    case AuthenticationType.NetworkCredentials:
                        {
                            if (!string.IsNullOrEmpty(_job.Credential))
                            {
                                jobRunnable.UseNetworkCredentialsAuthentication(_job.Credential);
                            }
                            else
                            {
                                jobRunnable.UseNetworkCredentialsAuthentication(_job.Username, _job.InsecurePassword, _job.Domain);
                            }
                            break;
                        }
                }
                foreach (var site in _job.Sites)
                {
                    jobRunnable.AddSite(site.Url);
                }
                try
                {
                    jobRunnable.Run();
                }
                catch (Exception ex)
                {
                    Exception = ex;
                }
                _job.LastRun = DateTime.Now.Ticks;
            }
            catch (TypeLoadException)
            {
                Log.Error(ApplicationStrings.ServiceIndentifier, "Could not load type");
            }
            catch (SerializationException)
            {
            }
            finally
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                }
            }
        }
    }
}
