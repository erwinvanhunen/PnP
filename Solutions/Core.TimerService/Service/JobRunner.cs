using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using OfficeDevPnP.Core.Framework.TimerJobs;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.Core.Utilities;

namespace OfficeDevPnP.TimerService
{
    public class JobRunner
    {
        public string Name { get; set; }
        public string Assembly { get; set; }

        public string Class { get; set; }

        public AuthenticationType AuthenticationType { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string AppId { get; set; }

        public string AppSecret { get; set; }
        public string CredentialManagerLabel { get; set; }

        public List<string> Sites { get; set; }

        public TimerJob TimerJob { get; set; }
        public DateTime LastRun { get; set; }

        public string TenantName { get; set; }

        public Guid Id { get; set; }
        public string Domain { get; set; }

        public Exception Exception { get; set; }

        public JobRunner()
        {
            Sites = new List<string>();
        }

        public void RunJob()
        {
            AppDomain appDomain = null;
            try
            {
                appDomain = AppDomain.CreateDomain("SPOTimerJob_" + Id);

                var jobRunnable = (TimerJob)appDomain.CreateInstanceFromAndUnwrap(Assembly, Class);

                switch (AuthenticationType)
                {
                    case AuthenticationType.Office365:
                        {
                            if (!string.IsNullOrEmpty(CredentialManagerLabel))
                            {
                                jobRunnable.UseOffice365Authentication(CredentialManagerLabel);
                            }
                            else
                            {
                                jobRunnable.UseOffice365Authentication(Username, Password);
                            }
                            break;
                        }
                    case AuthenticationType.AppOnly:
                        {

                            jobRunnable.UseAppOnlyAuthentication(AppId, AppSecret);

                            var wildcardused = false;
                            foreach (var site in Sites)
                            {
                                if (site.IndexOf("*") > -1)
                                {
                                    wildcardused = true;
                                    break;
                                }
                            }
                            if (wildcardused)
                            {
                                if (!string.IsNullOrEmpty(CredentialManagerLabel))
                                {
                                    jobRunnable.SetEnumerationCredentials(CredentialManagerLabel);
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(Domain))
                                    {
                                        jobRunnable.SetEnumerationCredentials(Username, Password, Domain);
                                    }
                                    else
                                    {
                                        jobRunnable.SetEnumerationCredentials(Username, Password);
                                    }
                                }
                            }
                            break;
                        }
                    case AuthenticationType.NetworkCredentials:
                        {
                            if (!string.IsNullOrEmpty(CredentialManagerLabel))
                            {
                                jobRunnable.UseNetworkCredentialsAuthentication(CredentialManagerLabel);
                            }
                            else
                            {
                                jobRunnable.UseNetworkCredentialsAuthentication(Username, Password, Domain);
                            }
                            break;
                        }
                }
                foreach (var url in Sites)
                {
                    jobRunnable.AddSite(url);
                }
                try
                {
                    jobRunnable.Run();
                }
                catch(Exception ex)
                {
                    Exception = ex;
                }
                LastRun = DateTime.Now;
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
