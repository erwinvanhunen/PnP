using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace OfficeDevPnP.TimerService.Domain
{
    //[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    //[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public class Jobs
    {
        /// <remarks/>

        private List<Job> _jobsCollection;

        /// <remarks/>
        [XmlElement("Job")]
        public List<Job> JobCollection
        {
            get
            {
                return _jobsCollection;
            }
            set
            {
                _jobsCollection = value;
            }
        }
        /// <remarks/>
        [XmlAttribute()]
        [DefaultValue(true)]
        public bool UseThreading { get; set; }


        public Jobs()
        {
            UseThreading = true;
            _jobsCollection = new List<Job>();
        }
    }
}

