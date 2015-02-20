using System;
using System.IO;
using System.Management.Automation;
using System.Xml.Linq;

namespace OfficeDevPnP.TimerService.Management
{
    public class BaseCmdlet : PSCmdlet
    {
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
                XDocument config = new XDocument();
                config.Add(new XElement("Jobs"));
                config.Save(_configFile);
            }
            return _configFile;
        }

        protected XDocument GetConfig()
        {
            var configFilePath = EnsureConfigFile();
            var configDocument = XDocument.Load(configFilePath);
            return configDocument;
        }



    }
}