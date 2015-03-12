using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Services;
using System.Security;
using OfficeDevPnP.Core.Framework.TimerJobs.Enums;
using OfficeDevPnP.TimerService.Enums;
using OfficeDevPnP.TimerService.Helpers;

namespace OfficeDevPnP.TimerService.Domain
{


    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class Job
    {
        public Job()
        {
            Sites = new List<JobSite>();
            MaxThreads = 5;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("Id")]
        public Guid Id { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("LastRun")]
        public long LastRun { get; set; }


        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("Time")]
        [DefaultValue(null)]
        public string Time { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("Day")]
        [DefaultValue(0)]
        public byte Day { get; set; }


        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("Site", IsNullable = false)]
        public List<JobSite> Sites { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(true)]
        public bool UseThreading { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(5)]
        public byte MaxThreads { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Name { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(false)]
        public bool Disabled { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Assembly { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Class { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ScheduleType ScheduleType { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(0)]
        public int Interval { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public AuthenticationType AuthenticationType { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string Credential { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string Username { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string Password { get; set; }

        public string GetInsecurePassword()
        {
            return Encryption.ToInsecureString(Encryption.DecryptString(Password));
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string Domain { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string AppId { get; set; }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(null)]
        public string AppSecret { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        [DefaultValue(false)]
        public bool IsRunning { get; set; }
    }



}
