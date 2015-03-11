using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Policy;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.TimerService.Domain;
using OfficeDevPnP.TimerService.Enums;
using OfficeDevPnP.TimerService.Helpers;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        private string _configFile;
        private const string Configfilename = "jobs.xml";

        [TestInitialize]
        public void Initialize()
        {
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP"));
            }
            _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "OfficeDevPnP", Configfilename);
        }

        [TestMethod]
        public void SerializeTest()
        {
            var jobs = new Jobs();

            var job = new Job() { Name = "test", Password = "blabla", ScheduleType = ScheduleType.Daily, Assembly = "Bla", Interval = 5, AuthenticationType = AuthenticationType.Office365 };
            job.Sites.Add(new JobSite() { Url = "bla" });
            jobs.JobCollection.Add(job);
            jobs.JobCollection.Add(job);
            XmlSerializer ser = new XmlSerializer(typeof(Jobs));
            using (TextWriter writer = new StreamWriter(_configFile))
            {
                ser.Serialize(writer, jobs);
            }
        }

        [TestMethod]
        public void DeserializeTest()
        {

            string xml = File.ReadAllText(_configFile);
            var jobs = xml.ParseXML<Jobs>();
            

            Assert.IsTrue(jobs.JobCollection.Count > 0);
        }

      
    }
}
