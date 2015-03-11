using System.Xml.Serialization;

namespace OfficeDevPnP.TimerService.Domain
{


    /// <remarks/>
    [XmlType(AnonymousType = true)]
    public class JobSite
    {
        /// <remarks/>
        [XmlAttribute()]
        public string Url { get; set; }
    }

}
