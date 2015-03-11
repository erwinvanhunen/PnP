using System;
using System.IO;
using System.Management.Automation;
using System.Xml.Linq;
using System.Xml.Serialization;
using OfficeDevPnP.TimerService.Domain;
using OfficeDevPnP.TimerService.Helpers;

namespace OfficeDevPnP.TimerService.Management
{
    public class BaseCmdlet : PSCmdlet
    {
        protected Jobs jobs;
        private readonly string _configFileName = "jobs.xml";

        private string EnsureConfigFile()
        {
       
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP"));
            }
            var _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP", _configFileName);
            if (!File.Exists(_configFile))
            {
                jobs = new Jobs();
                SaveJobs();
            }
            return _configFile;
        }

        protected void SaveJobs()
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
            using (TextWriter writer = new StreamWriter(EnsureConfigFile()))
            {
                ser.Serialize(writer, jobs);
            }
        }

        protected Jobs LoadJobs()
        {
            var xml = File.ReadAllText(EnsureConfigFile());
            return xml.ParseXML<Jobs>();
        }

        protected virtual void ExecuteCmdlet()
        { }

        protected override void ProcessRecord()
        {
            jobs = LoadJobs();
            ExecuteCmdlet();
        }
    }
}